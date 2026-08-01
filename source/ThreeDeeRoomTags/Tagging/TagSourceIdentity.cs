namespace ThreeDeeRoomTags.Tagging
{
    /// <summary>
    /// What a tag is a tag *of*: a spatial element, and the link instance it was read through.
    ///
    /// The element's own id is not enough. Two instances of one linked file share a single
    /// link document, so the same room hands back byte-identical ids through both — and a run
    /// for the second placement would find the first placement's tags, drag them across, and
    /// leave the first untagged. Nothing was reported, because as far as the matching was
    /// concerned it had found its tag.
    ///
    /// The link instance is therefore part of the identity. Host elements have no link, and
    /// their stored form is the bare element id, which is exactly what tags carried before —
    /// so every existing host tag still matches itself and nothing has to be migrated.
    ///
    /// A note on where this lives. The accepted design (openspec design.md) puts the two extra
    /// values in their own family parameters, SourceDocumentId and SourceLinkInstanceId. That
    /// needs the bundled .rfa and every family under revit/ to be edited in Revit and
    /// re-embedded, and until that happens a tool that wrote to those parameters would reject
    /// every family in the wild for not having them. Composing the identity into the existing
    /// SpatialElementId text parameter fixes the defect now, on families people already have.
    /// When the family does gain the parameters, ToStoredValue and TryParse are the only two
    /// places that need to know.
    /// </summary>
    internal readonly struct TagSourceIdentity : IEquatable<TagSourceIdentity>
    {
        /// <summary>
        /// Marks a stored value as carrying a link instance. Chosen so it cannot be mistaken
        /// for the bare element id that older tags carry: those are Revit UniqueIds, which are
        /// hex and hyphens and never contain a colon.
        /// </summary>
        private const string LinkPrefix = "link:";

        private const char Separator = '|';

        public TagSourceIdentity(string sourceId, string linkInstanceId)
        {
            SourceId = sourceId;
            LinkInstanceId = string.IsNullOrWhiteSpace(linkInstanceId) ? null : linkInstanceId;
        }

        /// <summary>The spatial element's own UniqueId, in whichever document it lives.</summary>
        public string SourceId { get; }

        /// <summary>The link instance it was read through, or null when it is a host element.</summary>
        public string LinkInstanceId { get; }

        public bool IsFromLink => LinkInstanceId != null;

        /// <summary>
        /// What goes on the tag. A host element stores its bare id, unchanged from what this
        /// tool has always written.
        /// </summary>
        public string ToStoredValue() =>
            IsFromLink ? LinkPrefix + LinkInstanceId + Separator + SourceId : SourceId;

        /// <summary>
        /// Reads back what a tag carries.
        /// </summary>
        /// <param name="isLegacy">
        /// True when the value is a bare element id with no link instance in it. For a host tag
        /// that is simply the current format; for a tag that came from a link it is a tag
        /// written before this identity existed, and a candidate for migration.
        /// </param>
        public static bool TryParse(string stored, out TagSourceIdentity identity, out bool isLegacy)
        {
            identity = default;
            isLegacy = false;

            if (string.IsNullOrWhiteSpace(stored)) return false;

            if (!stored.StartsWith(LinkPrefix, StringComparison.Ordinal))
            {
                identity = new TagSourceIdentity(stored, null);
                isLegacy = true;
                return true;
            }

            var body = stored.Substring(LinkPrefix.Length);
            var separator = body.IndexOf(Separator);

            // A prefix with nothing usable after it is not something this tool wrote. Treated
            // as unreadable rather than guessed at, so it matches nothing and is left alone.
            if (separator <= 0 || separator == body.Length - 1) return false;

            identity = new TagSourceIdentity(body.Substring(separator + 1), body.Substring(0, separator));
            return true;
        }

        public bool Equals(TagSourceIdentity other) =>
            string.Equals(SourceId, other.SourceId, StringComparison.Ordinal)
            && string.Equals(LinkInstanceId, other.LinkInstanceId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is TagSourceIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SourceId is null ? 0 : SourceId.GetHashCode();
                return (hash * 397) ^ (LinkInstanceId is null ? 0 : LinkInstanceId.GetHashCode());
            }
        }

        public override string ToString() => ToStoredValue();
    }
}
