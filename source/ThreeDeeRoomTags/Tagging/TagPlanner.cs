namespace ThreeDeeRoomTags.Tagging
{
    /// <summary>
    /// Decides what a run will do, before it does any of it.
    ///
    /// This is deliberately free of the Revit API. Planning used to be interleaved with
    /// placement inside one 110-line method, which meant no part of it could be exercised
    /// without a running Revit and a real document — so the rules about what gets skipped,
    /// what gets updated and what gets left alone were only ever checked by hand. They are
    /// the rules most likely to be wrong, and the ones a user notices when they are.
    /// </summary>
    internal static class TagPlanner
    {
        /// <summary>
        /// Works out, for each spatial element, whether it gets a new tag, an update to an
        /// existing one, or nothing.
        /// </summary>
        /// <param name="sources">The elements in scope, already in host coordinates.</param>
        /// <param name="existingTags">Tags already in the document. Ignored unless updating.</param>
        /// <param name="updateExisting">
        /// Whether to adopt matching tags. Off means every run places a fresh set, which is
        /// what the dialog offers and what leaves duplicates behind on purpose.
        /// </param>
        public static List<TagOperation> Plan(
            IReadOnlyList<SpatialElementSnapshot> sources,
            IReadOnlyList<ExistingTagSnapshot> existingTags,
            bool updateExisting)
        {
            var operations = new List<TagOperation>();

            if (sources is null) return operations;

            var tagsBySourceId = IndexBySourceId(updateExisting ? existingTags : null);

            foreach (var source in sources)
            {
                if (source is null) continue;

                // Order matters and matches what the placement loop did: an unplaced element
                // is rejected before its name is looked at, and both are rejected before any
                // existing tag is considered.
                if (!source.IsPlaced)
                {
                    operations.Add(Skip(source, SkipReason.NotPlaced));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(source.Name) || string.IsNullOrWhiteSpace(source.Number))
                {
                    operations.Add(Skip(source, SkipReason.MissingNameOrNumber));
                    continue;
                }

                if (tagsBySourceId.TryGetValue(source.SourceId ?? string.Empty, out var existing))
                {
                    if (!existing.IsEditable)
                    {
                        operations.Add(Skip(source, SkipReason.NotEditable));
                        continue;
                    }

                    operations.Add(new TagOperation
                    {
                        Kind = TagOperationKind.Update,
                        Source = source,
                        ExistingTagId = existing.TagId
                    });

                    continue;
                }

                operations.Add(new TagOperation
                {
                    Kind = TagOperationKind.Create,
                    Source = source
                });
            }

            return operations;
        }

        /// <summary>
        /// Tags keyed by the source id written on them.
        ///
        /// First one wins, which is what a linear search for the first match did before, so a
        /// model that somehow carries two tags for one room behaves as it always has. Building
        /// the index once also takes the matching from a scan per element to a lookup per
        /// element, which matters on a model with thousands of both.
        ///
        /// Tags with no stored id are left out entirely: they match nothing, and treating a
        /// null id as a key would let two of them collide with each other.
        /// </summary>
        private static Dictionary<string, ExistingTagSnapshot> IndexBySourceId(IReadOnlyList<ExistingTagSnapshot> existingTags)
        {
            var index = new Dictionary<string, ExistingTagSnapshot>(StringComparer.Ordinal);

            if (existingTags is null) return index;

            foreach (var tag in existingTags)
            {
                if (tag is null || string.IsNullOrEmpty(tag.StoredSourceId)) continue;

                if (!index.ContainsKey(tag.StoredSourceId)) index.Add(tag.StoredSourceId, tag);
            }

            return index;
        }

        private static TagOperation Skip(SpatialElementSnapshot source, SkipReason reason) => new TagOperation
        {
            Kind = TagOperationKind.Skip,
            Source = source,
            Reason = reason
        };
    }
}
