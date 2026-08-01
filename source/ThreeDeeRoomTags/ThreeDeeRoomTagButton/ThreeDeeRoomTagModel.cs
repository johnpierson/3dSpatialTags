using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Utilities;

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    /// <summary>What a tagging run actually did, so the dialog can say so.</summary>
    public class TaggingResult
    {
        public List<FamilyInstance> Tags { get; } = new List<FamilyInstance>();

        /// <summary>
        /// Existing tags that could not be touched — owned by another user, or changed in central.
        /// Counted and reported rather than replaced: placing a second tag on top of one somebody
        /// else owns leaves two tags in the model and a suppressed warning saying so.
        /// </summary>
        public int SkippedNotEditable { get; set; }

        /// <summary>
        /// Set when the chosen family cannot hold a value this tool writes. The run is rolled back
        /// whole rather than leaving half a model tagged with blanks.
        /// </summary>
        public string MissingParameter { get; set; }
    }

    public class ThreeDeeRoomTagModel
    {
        /// <summary>The parameters the bundled tag family carries, and this tool writes.</summary>
        private static readonly string[] RequiredTagParameters = { "Name", "Number", "SpatialElementId" };

        private const string TagFamilyName = "3dSpatialElementTag";

        public UIApplication UiApp { get; }
        public Document Doc { get; }
        public UIDocument UiDoc { get; }
        internal ThreeDeeRoomTagModel(UIApplication uiApp)
        {
            UiApp = uiApp;
            UiDoc = uiApp.ActiveUIDocument;
            Doc = uiApp.ActiveUIDocument.Document;
        }

        public ObservableCollection<Phase> CollectPhases()
        {
            return CollectPhases(Doc);
        }

        public ObservableCollection<Phase> CollectPhases(RevitLinkInstance linkInstance)
        {
            // A link can be unloaded between the dialog opening and a selection being made, and an
            // unloaded link has no document at all.
            var doc = linkInstance?.GetLinkDocument();

            return doc is null ? new ObservableCollection<Phase>() : CollectPhases(doc);
        }

        private static ObservableCollection<Phase> CollectPhases(Document doc)
        {
            List<Phase> phases = new FilteredElementCollector(doc).OfClass(typeof(Phase)).WhereElementIsNotElementType().Cast<Phase>().ToList();

            return new ObservableCollection<Phase>(phases);
        }

        public ObservableCollection<SpatialElement> CollectSpatialElements(Phase phase, int targetIndex)
        {
            return CollectSpatialElements(Doc, phase, targetIndex);
        }

        public ObservableCollection<SpatialElement> CollectSpatialElements(Phase phase, RevitLinkInstance linkInstance, int targetIndex)
        {
            var doc = linkInstance?.GetLinkDocument();

            return doc is null
                ? new ObservableCollection<SpatialElement>()
                : CollectSpatialElements(doc, phase, targetIndex);
        }

        private static ObservableCollection<SpatialElement> CollectSpatialElements(Document doc, Phase phase, int targetIndex)
        {
            if (phase is null)
            {
                return new ObservableCollection<SpatialElement>();
            }

            var category = targetIndex == 0 ? BuiltInCategory.OST_Rooms : BuiltInCategory.OST_MEPSpaces;

            var elements = new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .OfType<SpatialElement>()

                // Null-guarded, and compared from the phase's own id so a missing parameter is a
                // non-match rather than a null reference: an element in the category without a
                // phase parameter is not a room or space this tool understands.
                .Where(s => phase.Id.Equals(s.get_Parameter(BuiltInParameter.ROOM_PHASE)?.AsElementId()))
                .ToList();

            return new ObservableCollection<SpatialElement>(elements);
        }

        public ObservableCollection<FamilySymbol> CollectRoomTagFamilySymbols()
        {
            List<FamilySymbol> tags = FindTagSymbols();

            if (!tags.Any() && LoadFamily())
            {
                tags = FindTagSymbols();
            }

            return new ObservableCollection<FamilySymbol>(tags.OrderBy(f => f.Name));
        }

        private List<FamilySymbol> FindTagSymbols()
        {
            return new FilteredElementCollector(Doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .Where(f => f.Family.Name.Contains(TagFamilyName)).ToList();
        }

        /// <summary>
        /// The first parameter this tag instance is missing, or null if it carries all of them.
        ///
        /// The families on offer are found by a name match, so a user family called something like
        /// "3dSpatialElementTag-old" gets listed too. Whether it will take the values this tool
        /// writes can only be answered by a placed instance — the parameters are instance
        /// parameters, and a family symbol does not carry them.
        /// </summary>
        private static string FirstMissingParameter(FamilyInstance tag)
        {
            return RequiredTagParameters.FirstOrDefault(name => tag.LookupParameter(name) is null);
        }

        public bool LoadFamily()
        {
            string familyPath = string.Empty;
            string installPath = Path.Combine(Global.ExecutingPath, $"{TagFamilyName}.rfa");

            if (File.Exists(installPath))
            {
                familyPath = installPath;
            }
            else
            {
                try
                {
                    string tempPath = Path.Combine(Global.TempPath, $"{TagFamilyName}.rfa");
                    File.WriteAllBytes(tempPath, Properties.FamilySymbols._3dSpatialElementTag);

                    if (File.Exists(tempPath))
                    {
                        familyPath = tempPath;
                    }
                }
                catch (Exception)
                {
                    // An unwritable temp folder is not something the user can fix from here; the
                    // dialog reports the missing family instead.
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(familyPath)) return false;

            bool result;

            using (Transaction t = new Transaction(Doc, "Loading tag"))
            {
                t.Start();
                result = Doc.LoadFamily(familyPath);
                t.Commit();
            }

            return result;
        }

        public TaggingResult CreateRoomTags(FamilySymbol spatialElementTag, ObservableCollection<SpatialElement> spatialElements, bool updateExisting = true, RevitLinkInstance linkInstance = null)
        {
            var result = new TaggingResult();

            List<FamilyInstance> existingTags = new List<FamilyInstance>();

            if (updateExisting)
            {
                existingTags = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType().Cast<FamilyInstance>()
                    .Where(f => f.Symbol.Family.Name.Contains(TagFamilyName)).ToList();
            }

            var transform = linkInstance?.GetTransform();

            using (Transaction t = new Transaction(Doc, "Placing 3d spatial element tags"))
            {
                t.Start();

                if (!spatialElementTag.IsActive)
                {
                    spatialElementTag.Activate();
                }

                foreach (var spatialElement in spatialElements)
                {
                    var spatialElementLocation = spatialElement.Location as LocationPoint;

                    if (spatialElementLocation is null) continue;

                    var spatialElementPoint = spatialElementLocation.Point;

                    if (transform != null)
                    {
                        spatialElementPoint = transform.OfPoint(spatialElementPoint);
                    }

                    //if the room name and number are blank, then skip.
                    var roomName = spatialElement.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString();
                    var roomNumber = spatialElement.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString();

                    if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(roomNumber))
                    {
                        continue;
                    }

                    FamilyInstance roomTagInstance = null;

                    if (updateExisting)
                    {
                        // Null-safe on both sides: a tag whose id parameter was removed or never
                        // filled in reads back as null, and comparing against it threw.
                        var foundTag = existingTags.FirstOrDefault(f =>
                            string.Equals(f.LookupParameter("SpatialElementId")?.AsString(), spatialElement.UniqueId, StringComparison.Ordinal));

                        if (foundTag != null)
                        {
                            if (foundTag.IsElementEditable())
                            {
                                roomTagInstance = foundTag;
                                foundTag.Symbol = spatialElementTag;

                                if (foundTag.Location is LocationPoint tagLocation)
                                {
                                    tagLocation.Point = spatialElementPoint;
                                }
                            }
                            else
                            {
                                // Somebody else owns it. Placing a replacement on top would leave
                                // the model with two tags for one room, so this one is reported.
                                result.SkippedNotEditable++;
                                continue;
                            }
                        }
                    }

                    if (roomTagInstance == null)
                    {
                        roomTagInstance = Doc.Create.NewFamilyInstance(spatialElementPoint, spatialElementTag, StructuralType.NonStructural);
                    }

                    // Checked once, on the first tag, and the whole run is abandoned if the family
                    // cannot hold what this tool writes. Carrying on would leave a model full of
                    // tags with no room name in them and no way to match them up again.
                    if (result.Tags.Count == 0)
                    {
                        result.MissingParameter = FirstMissingParameter(roomTagInstance);

                        if (result.MissingParameter != null)
                        {
                            t.RollBack();
                            result.Tags.Clear();
                            result.SkippedNotEditable = 0;
                            return result;
                        }
                    }

                    roomTagInstance.LookupParameter("Name").Set(roomName);
                    roomTagInstance.LookupParameter("Number").Set(roomNumber);
                    roomTagInstance.LookupParameter("SpatialElementId").Set(spatialElement.UniqueId);

                    result.Tags.Add(roomTagInstance);
                }

                // Set failure handler to hide identical instances warnings which may be posted.
                FailureHandlingOptions failureOptions = t.GetFailureHandlingOptions();
                failureOptions.SetFailuresPreprocessor(new HideOverlappingElementWarning());
                t.Commit(failureOptions);
            }

            return result;
        }

        public ObservableCollection<RevitLinkInstance> GetRevitLinks()
        {
            // Unloaded links are left out: they have no document to read rooms from, and offering
            // one only leads to an empty phase list and no explanation.
            var links = new FilteredElementCollector(Doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>()
                .Where(l => l.GetLinkDocument() != null)
                .OrderBy(l => l.Name).ToList();

            return new ObservableCollection<RevitLinkInstance>(links);
        }

        public RevitLinkInstance IsLinkSelected()
        {
            var id = UiDoc.Selection.GetElementIds().FirstOrDefault();

            if (id is null)
            {
                return null;
            }

            return Doc.GetElement(id) as RevitLinkInstance;
        }
    }
}
