namespace ThreeDeeRoomTags.Classes
{
    class HideOverlappingElementWarning : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (var failure in failuresAccessor.GetFailureMessages())
            {
                if (failure.GetFailureDefinitionId() == BuiltInFailures.OverlapFailures.DuplicateInstances)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }

            // Handle any other errors interactively
            return FailureProcessingResult.Continue;
        }
    }
}
