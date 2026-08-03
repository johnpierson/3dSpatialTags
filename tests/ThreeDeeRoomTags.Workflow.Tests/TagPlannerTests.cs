using ThreeDeeRoomTags.Tagging;
using Xunit;

namespace ThreeDeeRoomTags.Workflow.Tests
{
    /// <summary>
    /// What a run decides to do, before it does any of it.
    ///
    /// These are the rules that decide whether somebody's model gets a tag, a moved tag, or
    /// nothing — and until the planner was pulled out of the placement loop, none of them
    /// could be checked without a running Revit.
    /// </summary>
    public class TagPlannerTests
    {
        private static SpatialElementSnapshot Room(
            string id,
            string name = "Office",
            string number = "101",
            bool placed = true,
            double area = 120,
            string link = null)
        {
            return new SpatialElementSnapshot
            {
                SourceId = id,
                LinkInstanceId = link,
                Name = name,
                Number = number,
                IsPlaced = placed,
                Point = new TagPoint(1, 2, 3),
                Area = area
            };
        }

        private static ExistingTagSnapshot Tag(
            string tagId,
            string storedSourceId,
            bool editable = true,
            bool sourceMissing = false,
            string sourceLinkInstanceId = null)
        {
            return new ExistingTagSnapshot
            {
                TagId = tagId,
                StoredSourceId = storedSourceId,
                SourceLinkInstanceId = sourceLinkInstanceId,
                IsEditable = editable,
                SourceMissing = sourceMissing
            };
        }

        /// <summary>What a tag for an element read through a link instance carries.</summary>
        private static string StoredFor(string sourceId, string link) =>
            new TagSourceIdentity(sourceId, link).ToStoredValue();

        private static List<ExistingTagSnapshot> NoTags() => new List<ExistingTagSnapshot>();

        [Fact]
        public void PlacesATagForAValidRoomWithNothingAlreadyThere()
        {
            var plan = TagPlanner.Plan(new[] { Room("room-1") }, NoTags(), updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Create, operation.Kind);
            Assert.Equal("room-1", operation.Source.SourceId);
        }

        [Fact]
        public void UpdatesTheTagThatCarriesAMatchingSourceId()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1") },
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Update, operation.Kind);
            Assert.Equal("tag-a", operation.ExistingTagId);
        }

        /// <summary>
        /// With updating off, the dialog promises "a fresh set every time" — so a matching tag
        /// is not adopted even though one exists.
        /// </summary>
        [Fact]
        public void CreatesRatherThanUpdatesWhenUpdatingIsOff()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1") },
                updateExisting: false);

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan.Operations).Kind);
        }

        /// <summary>
        /// Somebody else owns the tag, or it has moved on in central. Placing a replacement on
        /// top would leave the model with two tags for one room, so nothing is done and the
        /// skip is reported.
        /// </summary>
        [Fact]
        public void SkipsAndReportsATagOwnedBySomebodyElse()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1", editable: false) },
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Skip, operation.Kind);
            Assert.Equal(SkipReason.NotEditable, operation.Reason);
        }

        [Fact]
        public void SkipsAnUnplacedElement()
        {
            var plan = TagPlanner.Plan(new[] { Room("room-1", placed: false) }, NoTags(), updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Skip, operation.Kind);
            Assert.Equal(SkipReason.NotPlaced, operation.Reason);
        }

        [Theory]
        [InlineData(null, "101")]
        [InlineData("", "101")]
        [InlineData("   ", "101")]
        [InlineData("Office", null)]
        [InlineData("Office", "")]
        [InlineData("Office", "  ")]
        public void SkipsAnElementWithNothingToPutInTheTag(string name, string number)
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", name: name, number: number) },
                NoTags(),
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Skip, operation.Kind);
            Assert.Equal(SkipReason.MissingNameOrNumber, operation.Reason);
        }

        /// <summary>
        /// An unplaced element is rejected before its name is looked at. Pinned because the
        /// order decides which reason the user is told, and an unplaced room with a blank name
        /// is a real thing to find in a model.
        /// </summary>
        [Fact]
        public void PrefersTheUnplacedReasonOverTheBlankNameReason()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", name: null, placed: false) },
                NoTags(),
                updateExisting: true);

            Assert.Equal(SkipReason.NotPlaced, Assert.Single(plan.Operations).Reason);
        }

        /// <summary>
        /// A tag whose id parameter was removed or never filled in reads back as null. It
        /// matches nothing, and two such tags must not collide with each other.
        /// </summary>
        [Fact]
        public void IgnoresTagsThatCarryNoSourceId()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", null), Tag("tag-b", "") },
                updateExisting: true);

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan.Operations).Kind);
        }

        /// <summary>
        /// Two tags claiming one room is a broken model, but it is one that exists. The first
        /// is adopted, which is what a linear search for the first match did.
        /// </summary>
        [Fact]
        public void AdoptsTheFirstOfTwoTagsClaimingTheSameRoom()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-first", "room-1"), Tag("tag-second", "room-1") },
                updateExisting: true);

            Assert.Equal("tag-first", Assert.Single(plan.Operations).ExistingTagId);
        }

        [Fact]
        public void DecidesEachElementIndependently()
        {
            var plan = TagPlanner.Plan(
                new[]
                {
                    Room("room-1"),
                    Room("room-2", placed: false),
                    Room("room-3"),
                    Room("room-4", number: "")
                },
                new[] { Tag("tag-c", "room-3") },
                updateExisting: true);

            Assert.Equal(4, plan.Operations.Count);
            Assert.Equal(TagOperationKind.Create, plan.Operations[0].Kind);
            Assert.Equal(SkipReason.NotPlaced, plan.Operations[1].Reason);
            Assert.Equal(TagOperationKind.Update, plan.Operations[2].Kind);
            Assert.Equal(SkipReason.MissingNameOrNumber, plan.Operations[3].Reason);
        }

        /// <summary>
        /// An unbounded or redundant room reports zero area but is placed and carries the name
        /// and number Revit assigned it, so it is tagged. The dialog's warning says these
        /// "cannot be tagged", which is not what happens — pinned here so the two are settled
        /// deliberately rather than drifting apart again.
        /// </summary>
        [Fact]
        public void TagsAPlacedRoomWithNoArea()
        {
            var plan = TagPlanner.Plan(new[] { Room("room-1", area: 0) }, NoTags(), updateExisting: true);

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan.Operations).Kind);
        }

        // ------------------------------------------------------------------------------
        // Source identity: the defect this whole change exists for.
        // ------------------------------------------------------------------------------

        /// <summary>
        /// REGRESSION, finding TAG-01.
        ///
        /// One linked file placed twice. Both placements hand back the same room, with the
        /// same element id, because they share one link document. Tagging the second used to
        /// find the first's tags and drag them across — leaving the first placement untagged,
        /// on every run, with nothing reported.
        /// </summary>
        [Fact]
        public void DoesNotStealTheTagsOfAnotherPlacementOfTheSameLink()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", link: "link-B") },
                new[] { Tag("tag-for-A", StoredFor("room-1", "link-A")) },
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Create, operation.Kind);
            Assert.Null(operation.ExistingTagId);
        }

        [Fact]
        public void UpdatesTheTagBelongingToItsOwnLinkInstance()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", link: "link-B") },
                new[]
                {
                    Tag("tag-for-A", StoredFor("room-1", "link-A")),
                    Tag("tag-for-B", StoredFor("room-1", "link-B"))
                },
                updateExisting: true);

            Assert.Equal("tag-for-B", Assert.Single(plan.Operations).ExistingTagId);
        }

        /// <summary>
        /// A host element and a linked element that happen to share an id are still two
        /// different things. This is the same guarantee as above, from the other direction.
        /// </summary>
        [Fact]
        public void AHostElementDoesNotMatchALinkedTag()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-linked", StoredFor("room-1", "link-A")) },
                updateExisting: true);

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan.Operations).Kind);
        }

        /// <summary>
        /// Matching reads the family's own link-instance parameter, so a tag identified purely
        /// by what a schedule would show still resolves to its own placement.
        /// </summary>
        [Fact]
        public void MatchesOnTheFamilyLinkParameterWhenItIsSet()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", link: "link-B") },
                new[]
                {
                    Tag("tag-for-A", "room-1", sourceLinkInstanceId: "link-A"),
                    Tag("tag-for-B", "room-1", sourceLinkInstanceId: "link-B")
                },
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Update, operation.Kind);
            Assert.Equal("tag-for-B", operation.ExistingTagId);
            Assert.False(operation.IsLegacyMigration);
        }

        /// <summary>
        /// A tag whose family records a different placement is not a legacy tag going spare,
        /// even though its composite value is a bare id.
        /// </summary>
        [Fact]
        public void DoesNotMigrateATagThatAlreadyNamesAnotherPlacement()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", link: "link-B") },
                new[] { Tag("tag-for-A", "room-1", sourceLinkInstanceId: "link-A") },
                updateExisting: true);

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan.Operations).Kind);
        }

        // ------------------------------------------------------------------------------
        // Migrating tags written before the link instance was part of the identity.
        // ------------------------------------------------------------------------------

        /// <summary>
        /// Every tag in every model in the wild carries the bare element id. A linked run
        /// adopts one rather than placing a duplicate on top of it — otherwise upgrading would
        /// hand every existing user a second set of tags.
        /// </summary>
        [Fact]
        public void AdoptsALegacyTagForALinkedElementAndMarksItMigrated()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1", link: "link-A") },
                new[] { Tag("tag-legacy", "room-1") },
                updateExisting: true);

            var operation = Assert.Single(plan.Operations);
            Assert.Equal(TagOperationKind.Update, operation.Kind);
            Assert.Equal("tag-legacy", operation.ExistingTagId);
            Assert.True(operation.IsLegacyMigration);
        }

        /// <summary>
        /// A host tag's stored form has not changed, so adopting one is just an ordinary
        /// update — nothing is being migrated and the user should not be told it was.
        /// </summary>
        [Fact]
        public void AdoptingAHostTagIsNotAMigration()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1") },
                updateExisting: true);

            Assert.False(Assert.Single(plan.Operations).IsLegacyMigration);
        }

        /// <summary>
        /// The upgrade path, end to end. There is one legacy tag and the same link is placed
        /// twice; only one placement can honestly claim it, and the other gets its own tag
        /// rather than fighting over it.
        /// </summary>
        [Fact]
        public void OnlyOnePlacementCanClaimASingleLegacyTag()
        {
            var plan = TagPlanner.Plan(
                new[]
                {
                    Room("room-1", link: "link-A"),
                    Room("room-1", link: "link-B")
                },
                new[] { Tag("tag-legacy", "room-1") },
                updateExisting: true);

            Assert.Equal(2, plan.Operations.Count);
            Assert.Equal(TagOperationKind.Update, plan.Operations[0].Kind);
            Assert.True(plan.Operations[0].IsLegacyMigration);
            Assert.Equal(TagOperationKind.Create, plan.Operations[1].Kind);
        }

        [Fact]
        public void DoesNotAdoptALegacyTagTwiceInOneRun()
        {
            var plan = TagPlanner.Plan(
                new[]
                {
                    Room("room-1", link: "link-A"),
                    Room("room-1", link: "link-A")
                },
                new[] { Tag("tag-legacy", "room-1") },
                updateExisting: true);

            Assert.Equal(1, plan.Operations.Count(o => o.Kind == TagOperationKind.Update));
            Assert.Equal(1, plan.Operations.Count(o => o.Kind == TagOperationKind.Create));
        }

        // ------------------------------------------------------------------------------
        // Tags whose element is gone.
        // ------------------------------------------------------------------------------

        /// <summary>
        /// Finding TAG-02. A deleted room leaves its tag behind, still saying what it said, as
        /// model geometry that gets exported into coordination models. Counted so the user is
        /// told; not deleted, because removing elements uninvited is not this tool's business.
        /// </summary>
        [Fact]
        public void CountsTagsWhoseElementIsGone()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[]
                {
                    Tag("tag-a", "room-1"),
                    Tag("tag-gone", "room-deleted", sourceMissing: true),
                    Tag("tag-also-gone", "room-also-deleted", sourceMissing: true)
                },
                updateExisting: true);

            Assert.Equal(2, plan.OrphanedTagCount);
        }

        /// <summary>
        /// A tag this run just updated has a live element in front of it, whatever the caller
        /// said — belt and braces against reporting a number that would badly mislead.
        /// </summary>
        [Fact]
        public void DoesNotCountATagItJustAdoptedAsAnOrphan()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1", sourceMissing: true) },
                updateExisting: true);

            Assert.Equal(0, plan.OrphanedTagCount);
        }

        [Fact]
        public void ReportsNoOrphansWhenNothingIsMissing()
        {
            var plan = TagPlanner.Plan(
                new[] { Room("room-1") },
                new[] { Tag("tag-a", "room-1"), Tag("tag-b", "room-2") },
                updateExisting: true);

            Assert.Equal(0, plan.OrphanedTagCount);
        }

        [Fact]
        public void ReturnsNothingForNoElements()
        {
            Assert.Empty(TagPlanner.Plan(new SpatialElementSnapshot[0], NoTags(), updateExisting: true).Operations);
            Assert.Empty(TagPlanner.Plan(null, NoTags(), updateExisting: true).Operations);
        }
    }
}
