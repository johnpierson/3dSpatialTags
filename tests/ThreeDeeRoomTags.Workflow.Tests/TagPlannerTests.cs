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
            double area = 120)
        {
            return new SpatialElementSnapshot
            {
                SourceId = id,
                Name = name,
                Number = number,
                IsPlaced = placed,
                Point = new TagPoint(1, 2, 3),
                Area = area
            };
        }

        private static ExistingTagSnapshot Tag(string tagId, string storedSourceId, bool editable = true)
        {
            return new ExistingTagSnapshot
            {
                TagId = tagId,
                StoredSourceId = storedSourceId,
                IsEditable = editable
            };
        }

        private static List<ExistingTagSnapshot> NoTags() => new List<ExistingTagSnapshot>();

        [Fact]
        public void PlacesATagForAValidRoomWithNothingAlreadyThere()
        {
            var plan = TagPlanner.Plan(new[] { Room("room-1") }, NoTags(), updateExisting: true);

            var operation = Assert.Single(plan);
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

            var operation = Assert.Single(plan);
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

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan).Kind);
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

            var operation = Assert.Single(plan);
            Assert.Equal(TagOperationKind.Skip, operation.Kind);
            Assert.Equal(SkipReason.NotEditable, operation.Reason);
        }

        [Fact]
        public void SkipsAnUnplacedElement()
        {
            var plan = TagPlanner.Plan(new[] { Room("room-1", placed: false) }, NoTags(), updateExisting: true);

            var operation = Assert.Single(plan);
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

            var operation = Assert.Single(plan);
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

            Assert.Equal(SkipReason.NotPlaced, Assert.Single(plan).Reason);
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

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan).Kind);
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

            Assert.Equal("tag-first", Assert.Single(plan).ExistingTagId);
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

            Assert.Equal(4, plan.Count);
            Assert.Equal(TagOperationKind.Create, plan[0].Kind);
            Assert.Equal(SkipReason.NotPlaced, plan[1].Reason);
            Assert.Equal(TagOperationKind.Update, plan[2].Kind);
            Assert.Equal(SkipReason.MissingNameOrNumber, plan[3].Reason);
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

            Assert.Equal(TagOperationKind.Create, Assert.Single(plan).Kind);
        }

        [Fact]
        public void ReturnsNothingForNoElements()
        {
            Assert.Empty(TagPlanner.Plan(new SpatialElementSnapshot[0], NoTags(), updateExisting: true));
            Assert.Empty(TagPlanner.Plan(null, NoTags(), updateExisting: true));
        }
    }
}
