using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Tagging;
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
        /// <summary>
        /// The parameter a tag stores its source element's id in. This is the whole basis of
        /// matching a tag back to its room on a later run, so it is named once here rather
        /// than spelled out at each of the places that read and write it.
        /// </summary>
        internal const string SpatialElementIdParameter = "SpatialElementId";

        internal const string NameParameter = "Name";
        internal const string NumberParameter = "Number";

        /// <summary>The parameter the text height is written to, on the family type.</summary>
        internal const string TextHeightParameter = "Text Height";

        /// <summary>The parameters the bundled tag family carries, and this tool writes.</summary>
        private static readonly string[] RequiredTagParameters = { NameParameter, NumberParameter, SpatialElementIdParameter };

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

        /// <summary>
        /// Reads a spatial element into the plain data the planner works on, applying the link
        /// transform on the way so that everything downstream is in host coordinates.
        /// </summary>
        private static SpatialElementSnapshot Snapshot(SpatialElement spatialElement, Transform transform)
        {
            var location = spatialElement.Location as LocationPoint;
            var point = location?.Point;

            if (point != null && transform != null)
            {
                point = transform.OfPoint(point);
            }

            return new SpatialElementSnapshot
            {
                SourceId = spatialElement.UniqueId,
                Name = spatialElement.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString(),
                Number = spatialElement.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString(),
                IsPlaced = location != null,
                Point = point is null ? default : new TagPoint(point.X, point.Y, point.Z),
                Area = spatialElement.Area
            };
        }

        /// <summary>
        /// Reads an existing tag into plain data. The editability question is asked here, once
        /// per tag, rather than in the middle of placing things.
        /// </summary>
        private static ExistingTagSnapshot Snapshot(FamilyInstance tag) => new ExistingTagSnapshot
        {
            TagId = tag.UniqueId,

            // Null-safe: a tag whose id parameter was removed or never filled in reads back as
            // null, and comparing against it threw.
            StoredSourceId = tag.LookupParameter(SpatialElementIdParameter)?.AsString(),
            IsEditable = tag.IsElementEditable()
        };

        private List<FamilyInstance> CollectExistingTags()
        {
            return new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType().Cast<FamilyInstance>()
                .Where(f => f.Symbol.Family.Name.Contains(TagFamilyName)).ToList();
        }

        public TaggingResult CreateRoomTags(FamilySymbol spatialElementTag, ObservableCollection<SpatialElement> spatialElements, bool updateExisting = true, RevitLinkInstance linkInstance = null)
        {
            var result = new TaggingResult();

            List<FamilyInstance> existingTags = updateExisting ? CollectExistingTags() : new List<FamilyInstance>();

            var transform = linkInstance?.GetTransform();

            // Decided in full before the transaction opens, so that what the run intends to do
            // is a value that can be inspected and tested rather than a shape that only exists
            // while a document is being written to.
            var sourcesById = spatialElements.ToDictionary(s => s.UniqueId, s => s, StringComparer.Ordinal);
            var tagsById = existingTags.ToDictionary(t => t.UniqueId, t => t, StringComparer.Ordinal);

            var plan = TagPlanner.Plan(
                spatialElements.Select(s => Snapshot(s, transform)).ToList(),
                existingTags.Select(Snapshot).ToList(),
                updateExisting);

            using (Transaction t = new Transaction(Doc, "Placing 3d spatial element tags"))
            {
                t.Start();

                if (!spatialElementTag.IsActive)
                {
                    spatialElementTag.Activate();
                }

                foreach (var operation in plan)
                {
                    if (operation.Kind == TagOperationKind.Skip)
                    {
                        // Somebody else owns it. Placing a replacement on top would leave the
                        // model with two tags for one room, so this one is reported. The other
                        // skip reasons are nothing to report: they describe elements that could
                        // never carry a tag.
                        if (operation.Reason == SkipReason.NotEditable) result.SkippedNotEditable++;

                        continue;
                    }

                    var source = operation.Source;
                    var spatialElement = sourcesById[source.SourceId];
                    var spatialElementPoint = new XYZ(source.Point.X, source.Point.Y, source.Point.Z);

                    FamilyInstance roomTagInstance;

                    if (operation.Kind == TagOperationKind.Update)
                    {
                        roomTagInstance = tagsById[operation.ExistingTagId];
                        roomTagInstance.Symbol = spatialElementTag;

                        if (roomTagInstance.Location is LocationPoint tagLocation)
                        {
                            tagLocation.Point = spatialElementPoint;
                        }
                    }
                    else
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

                    roomTagInstance.LookupParameter(NameParameter).Set(source.Name);
                    roomTagInstance.LookupParameter(NumberParameter).Set(source.Number);
                    roomTagInstance.LookupParameter(SpatialElementIdParameter).Set(spatialElement.UniqueId);

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
