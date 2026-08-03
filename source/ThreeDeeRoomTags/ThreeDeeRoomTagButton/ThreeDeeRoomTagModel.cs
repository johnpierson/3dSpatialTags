using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Serilog;
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

        /// <summary>
        /// Tags written before the link instance was part of a tag's identity, adopted by this
        /// run and rewritten with it. Reported because it explains a one-off run where tags
        /// were updated rather than created, and because it only ever happens once.
        /// </summary>
        public int MigratedLegacyTags { get; set; }

        /// <summary>
        /// Tags whose spatial element has been deleted. Reported rather than removed: the tag
        /// is still model geometry in somebody's project, and deleting elements without being
        /// asked is not this tool's business. Their text is now wrong, which is worth saying.
        /// </summary>
        public int OrphanedTags { get; set; }

        /// <summary>
        /// Duplicate-instance warnings hidden because this run placed tags on top of its own
        /// previous ones. Only ever non-zero with updating turned off.
        /// </summary>
        public int SuppressedDuplicateWarnings { get; set; }
    }

    public class ThreeDeeRoomTagModel
    {
        /// <summary>
        /// The parameter a tag stores its source element's id in. This is the whole basis of
        /// matching a tag back to its room on a later run, so it is named once here rather
        /// than spelled out at each of the places that read and write it.
        /// </summary>
        internal const string SpatialElementIdParameter = "SpatialElementId";

        /// <summary>
        /// Where the tag records its source in a form Revit can schedule and filter on.
        ///
        /// Written when the family carries them, and read in preference to the composite value
        /// above. They are not in <see cref="RequiredTagParameters"/> on purpose: a family
        /// without them still works, it just cannot be scheduled by source, and rejecting one
        /// would turn a reporting feature into a hard incompatibility.
        /// </summary>
        internal const string SourceDocumentIdParameter = "SourceDocumentId";

        internal const string SourceLinkInstanceIdParameter = "SourceLinkInstanceId";

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

        /// <summary>The manifest name the build embeds this configuration's family under.</summary>
        private const string TagFamilyResource = "ThreeDeeRoomTags.Resources.3dSpatialElementTag.rfa";

        public bool LoadFamily()
        {
            // Extracted to a uniquely named DIRECTORY, with the file inside it keeping the
            // family's own name. Revit names a loaded family after the file it came from, so
            // putting the unique part in the filename produced families called
            // "3dSpatialElementTag.4622231a00da48d386fdcf38185da90b" — a new one on every load,
            // piling up in the model and filling the family drop-down with noise.
            //
            // The uniqueness is still worth having. It is not preferring an .rfa found beside
            // the assembly that matters most — nothing installs one there, so any file at that
            // path came from somewhere else, and for a MultiUser install "there" is under
            // ProgramData where any user on the machine could drop one — but a fixed temp path
            // is also a file another process can be sitting on when this one needs it.
            string familyDirectory;
            string familyPath;

            try
            {
                familyDirectory = Path.Combine(Global.TempPath, "3dSpatialTags", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(familyDirectory);

                familyPath = Path.Combine(familyDirectory, $"{TagFamilyName}.rfa");

                using (var stream = Global.ExecutingAssembly.GetManifestResourceStream(TagFamilyResource))
                {
                    if (stream is null)
                    {
                        Log.Error("The tag family resource {Resource} is not embedded in this build", TagFamilyResource);
                        return false;
                    }

                    using (var file = File.Create(familyPath))
                    {
                        stream.CopyTo(file);
                    }
                }
            }
            catch (Exception ex)
            {
                // An unwritable temp folder is not something the user can fix from here; the
                // dialog reports the missing family instead. The cause goes to the log,
                // because "no tag family is loaded" on its own has sent people looking in
                // entirely the wrong place.
                Log.Warning(ex, "Could not extract the bundled tag family to {TempPath}", Global.TempPath);
                return false;
            }

            bool result;

            try
            {
                using (Transaction t = new Transaction(Doc, "Loading tag"))
                {
                    t.Start();
                    result = Doc.LoadFamily(familyPath);
                    t.Commit();
                }
            }
            finally
            {
                // The extracted copy has served its purpose either way. It used to be written
                // to a fixed name and left behind on every run.
                try
                {
                    Directory.Delete(familyDirectory, true);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Could not remove the extracted tag family {Path}", familyDirectory);
                }
            }

            return result;
        }

        /// <summary>
        /// Reads a spatial element into the plain data the planner works on, applying the link
        /// transform on the way so that everything downstream is in host coordinates.
        /// </summary>
        private static SpatialElementSnapshot Snapshot(SpatialElement spatialElement, Transform transform, string linkInstanceId)
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
                LinkInstanceId = linkInstanceId,
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
        /// <param name="sourceDoc">
        /// The document the current run reads elements from, used to notice tags whose element
        /// has been deleted. Null when it cannot be reached, in which case no tag is called an
        /// orphan — an unreachable document is not evidence that anything is gone.
        /// </param>
        /// <param name="linkInstanceId">The link instance being tagged, or null for the host.</param>
        private static ExistingTagSnapshot Snapshot(FamilyInstance tag, Document sourceDoc, string linkInstanceId)
        {
            // Null-safe: a tag whose id parameter was removed or never filled in reads back as
            // null, and comparing against it threw.
            var stored = tag.LookupParameter(SpatialElementIdParameter)?.AsString();

            var storedLink = tag.LookupParameter(SourceLinkInstanceIdParameter)?.AsString();

            return new ExistingTagSnapshot
            {
                TagId = tag.UniqueId,
                StoredSourceId = stored,
                SourceLinkInstanceId = storedLink,
                IsEditable = tag.IsElementEditable(),
                SourceMissing = IsSourceMissing(stored, storedLink, sourceDoc, linkInstanceId)
            };
        }

        /// <summary>
        /// Whether a tag names an element that is no longer in the document it came from.
        ///
        /// Deliberately narrow. Only tags belonging to the scope being tagged are considered,
        /// because a tag for a room in another phase, or read through a different link
        /// instance, has not been orphaned just because this run is not about it — calling it
        /// one would report a number that frightens people about nothing.
        /// </summary>
        private static bool IsSourceMissing(string stored, string storedLink, Document sourceDoc, string linkInstanceId)
        {
            if (sourceDoc is null) return false;
            if (!TagSourceIdentity.TryResolve(stored, storedLink, out var identity, out var isLegacy)) return false;

            // A legacy tag carries no link instance, so there is no way to tell which scope it
            // belongs to. Left alone rather than guessed at.
            if (isLegacy && linkInstanceId != null) return false;

            if (!string.Equals(identity.LinkInstanceId, linkInstanceId, StringComparison.Ordinal)) return false;

            try
            {
                return sourceDoc.GetElement(identity.SourceId) is null;
            }
            catch (Exception)
            {
                // A malformed id is not proof of a deleted element.
                return false;
            }
        }

        private List<FamilyInstance> CollectExistingTags()
        {
            return new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType().Cast<FamilyInstance>()
                .Where(f => f.Symbol.Family.Name.Contains(TagFamilyName)).ToList();
        }

        /// <summary>
        /// A whole run: the family type's text height, and the tags, as one thing.
        ///
        /// The two used to be separate committed transactions with nothing tying them
        /// together, so a run that reported "Nothing was placed" had already changed the
        /// family type's text height and left it changed. A successful run also arrived as two
        /// undo entries, and the first Ctrl+Z took back only the tags.
        ///
        /// A TransactionGroup makes the pair atomic and, assimilated, a single undo step.
        /// </summary>
        /// <param name="textHeightInches">
        /// The height to apply, or zero or less to leave the family's own alone.
        /// </param>
        public TaggingResult RunTagging(
            FamilySymbol spatialElementTag,
            ObservableCollection<SpatialElement> spatialElements,
            bool updateExisting,
            RevitLinkInstance linkInstance,
            double textHeightInches)
        {
            using (var group = new TransactionGroup(Doc, "Create / update 3d spatial tags"))
            {
                group.Start();

                try
                {
                    ApplyTextHeight(spatialElementTag, textHeightInches);

                    var result = CreateRoomTags(spatialElementTag, spatialElements, updateExisting, linkInstance);

                    // The family could not hold what this tool writes, so the tagging
                    // transaction rolled itself back. The height change goes with it: reporting
                    // that nothing was placed while having quietly resized the family type is
                    // the partial mutation this group exists to prevent.
                    if (result.MissingParameter != null)
                    {
                        group.RollBack();
                        return result;
                    }

                    group.Assimilate();

                    return result;
                }
                catch (Exception)
                {
                    group.RollBack();
                    throw;
                }
            }
        }

        /// <summary>
        /// Pushes the requested text height onto the tag family type, if one was asked for and
        /// the family can take it. A family without the parameter is not a failure worth
        /// stopping the run over — the tags are still correct, they are just the size the
        /// family already was.
        /// </summary>
        private void ApplyTextHeight(FamilySymbol famSymb, double inches)
        {
            if (inches <= 0) return;

            var param = famSymb.LookupParameter(TextHeightParameter);

            if (param is null || param.IsReadOnly || param.StorageType != StorageType.Double) return;

            double feet = inches / 12;

            if (Math.Abs(param.AsDouble() - feet) < 1e-9) return;

            using (Transaction t = new Transaction(Doc, "Setting Tag Height"))
            {
                t.Start();
                param.Set(feet);
                t.Commit();
            }
        }

        public TaggingResult CreateRoomTags(FamilySymbol spatialElementTag, ObservableCollection<SpatialElement> spatialElements, bool updateExisting = true, RevitLinkInstance linkInstance = null)
        {
            var result = new TaggingResult();

            List<FamilyInstance> existingTags = updateExisting ? CollectExistingTags() : new List<FamilyInstance>();

            // GetTotalTransform, not GetTransform: a link nested inside another link is placed
            // by the composition of both, and its own transform alone would put the tags in
            // the wrong place. The two are identical for a directly placed link.
            var transform = linkInstance?.GetTotalTransform();
            var linkInstanceId = linkInstance?.UniqueId;
            var sourceDoc = linkInstance is null ? Doc : linkInstance.GetLinkDocument();

            // Which file the rooms were read out of, recorded so a schedule can say so. The
            // path is what identifies it to a person; an unsaved document has only a title.
            var sourceDocumentId = string.IsNullOrWhiteSpace(sourceDoc?.PathName)
                ? sourceDoc?.Title ?? string.Empty
                : sourceDoc.PathName;

            // Decided in full before the transaction opens, so that what the run intends to do
            // is a value that can be inspected and tested rather than a shape that only exists
            // while a document is being written to.
            var sourcesById = spatialElements.ToDictionary(s => s.UniqueId, s => s, StringComparer.Ordinal);
            var tagsById = existingTags.ToDictionary(t => t.UniqueId, t => t, StringComparer.Ordinal);

            var plan = TagPlanner.Plan(
                spatialElements.Select(s => Snapshot(s, transform, linkInstanceId)).ToList(),
                existingTags.Select(t => Snapshot(t, sourceDoc, linkInstanceId)).ToList(),
                updateExisting);

            result.OrphanedTags = plan.OrphanedTagCount;

            using (Transaction t = new Transaction(Doc, "Placing 3d spatial element tags"))
            {
                t.Start();

                if (!spatialElementTag.IsActive)
                {
                    spatialElementTag.Activate();
                }

                foreach (var operation in plan.Operations)
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

                        if (operation.IsLegacyMigration) result.MigratedLegacyTags++;
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

                    // The element AND the link instance it came from. A tag adopted from the
                    // old bare-id format is rewritten here, so the next run matches it exactly
                    // and a second placement of the same link no longer finds it to steal.
                    roomTagInstance.LookupParameter(SpatialElementIdParameter).Set(source.Identity.ToStoredValue());

                    // The same identity again, split into the parameters the family carries for
                    // it, where Revit can schedule and filter on it. Set only if the family has
                    // them: an older family is still perfectly usable without.
                    roomTagInstance.LookupParameter(SourceLinkInstanceIdParameter)?.Set(source.LinkInstanceId ?? string.Empty);
                    roomTagInstance.LookupParameter(SourceDocumentIdParameter)?.Set(sourceDocumentId);

                    result.Tags.Add(roomTagInstance);
                }

                // Suppress the duplicate-instance warning for tags this run stacked on its own
                // previous ones — which is what "place a fresh set every time" means — and
                // nothing else. The preprocessor is given the ids it is allowed to silence.
                var preprocessor = new HideOverlappingElementWarning(result.Tags.Select(tag => tag.Id));

                FailureHandlingOptions failureOptions = t.GetFailureHandlingOptions();
                failureOptions.SetFailuresPreprocessor(preprocessor);
                t.Commit(failureOptions);

                result.SuppressedDuplicateWarnings = preprocessor.SuppressedDuplicateWarnings;
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
