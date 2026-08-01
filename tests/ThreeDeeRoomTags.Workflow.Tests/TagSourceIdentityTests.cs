using ThreeDeeRoomTags.Tagging;
using Xunit;

namespace ThreeDeeRoomTags.Workflow.Tests
{
    /// <summary>
    /// What a tag records about where it came from, and how it reads back.
    ///
    /// The round trip matters more than it looks: every tag already in every model in the wild
    /// carries the legacy form, and a reader that got that wrong would either orphan all of
    /// them or adopt the wrong ones.
    /// </summary>
    public class TagSourceIdentityTests
    {
        private const string RoomId = "9c1f2a70-0000-0000-0000-000000000001-0004a2b1";
        private const string LinkA = "aaaa1111-0000-0000-0000-000000000001-0000c0de";
        private const string LinkB = "bbbb2222-0000-0000-0000-000000000002-0000c0de";

        [Fact]
        public void AHostElementStoresItsBareId()
        {
            var identity = new TagSourceIdentity(RoomId, null);

            Assert.False(identity.IsFromLink);
            Assert.Equal(RoomId, identity.ToStoredValue());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AnAbsentLinkIsTreatedAsNoLink(string linkInstanceId)
        {
            Assert.False(new TagSourceIdentity(RoomId, linkInstanceId).IsFromLink);
        }

        [Fact]
        public void ALinkedElementRoundTrips()
        {
            var stored = new TagSourceIdentity(RoomId, LinkA).ToStoredValue();

            Assert.True(TagSourceIdentity.TryParse(stored, out var parsed, out var isLegacy));
            Assert.False(isLegacy);
            Assert.Equal(RoomId, parsed.SourceId);
            Assert.Equal(LinkA, parsed.LinkInstanceId);
        }

        /// <summary>
        /// The whole point of the change: one room, read through two placements of the same
        /// linked file, is two different things to tag.
        /// </summary>
        [Fact]
        public void TheSameRoomThroughTwoLinkInstancesIsTwoIdentities()
        {
            var throughA = new TagSourceIdentity(RoomId, LinkA);
            var throughB = new TagSourceIdentity(RoomId, LinkB);

            Assert.NotEqual(throughA, throughB);
            Assert.NotEqual(throughA.ToStoredValue(), throughB.ToStoredValue());
        }

        [Fact]
        public void EqualIdentitiesAgreeOnHashCode()
        {
            var one = new TagSourceIdentity(RoomId, LinkA);
            var other = new TagSourceIdentity(RoomId, LinkA);

            Assert.Equal(one, other);
            Assert.Equal(one.GetHashCode(), other.GetHashCode());
        }

        /// <summary>
        /// Every tag placed by an earlier version carries this, and it has to keep reading as
        /// the host element it always was.
        /// </summary>
        [Fact]
        public void ABareIdReadsAsLegacyWithNoLink()
        {
            Assert.True(TagSourceIdentity.TryParse(RoomId, out var parsed, out var isLegacy));
            Assert.True(isLegacy);
            Assert.Equal(RoomId, parsed.SourceId);
            Assert.Null(parsed.LinkInstanceId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("link:")]
        [InlineData("link:onlyalink")]
        [InlineData("link:|no-link-part")]
        [InlineData("link:no-source-part|")]
        public void UnreadableValuesMatchNothing(string stored)
        {
            Assert.False(TagSourceIdentity.TryParse(stored, out _, out _));
        }
    }
}
