namespace ThreeDeeRoomTags.Classes
{
    /// <summary>
    /// Hides the duplicate-instance warning for tags this run placed on top of each other, and
    /// only those.
    ///
    /// It used to delete every duplicate-instance warning raised at commit, whatever raised it.
    /// A tagging run can surface warnings about geometry it had nothing to do with, and
    /// silencing those is worse than the noise: a coordination model quietly losing its
    /// duplicate warnings is exactly the kind of data-quality erosion this tool is supposed to
    /// help with. The project's own design note says the broad suppression has to go.
    ///
    /// What is legitimately suppressed is the tool arguing with itself. With "update the ones
    /// already placed" unticked, the dialog promises a fresh set every time, which means
    /// placing tags on top of the previous ones on purpose; Revit says so once per pair, and
    /// the user already knows.
    /// </summary>
    internal class HideOverlappingElementWarning : IFailuresPreprocessor
    {
        private readonly HashSet<ElementId> _ownTags;

        /// <param name="ownTags">The tags this run created or updated.</param>
        public HideOverlappingElementWarning(IEnumerable<ElementId> ownTags)
        {
            _ownTags = new HashSet<ElementId>(ownTags ?? Enumerable.Empty<ElementId>());
        }

        /// <summary>How many warnings were suppressed, so the run can say so.</summary>
        public int SuppressedDuplicateWarnings { get; private set; }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (var failure in failuresAccessor.GetFailureMessages())
            {
                if (failure.GetFailureDefinitionId() != BuiltInFailures.OverlapFailures.DuplicateInstances) continue;

                var failing = failure.GetFailingElementIds();

                // Every element the warning names has to be one of ours. A warning that
                // involves anything else is somebody's real problem and stays on screen.
                if (failing.Count == 0 || !failing.All(id => _ownTags.Contains(id))) continue;

                failuresAccessor.DeleteWarning(failure);
                SuppressedDuplicateWarnings++;
            }

            // Handle any other errors interactively
            return FailureProcessingResult.Continue;
        }
    }
}
