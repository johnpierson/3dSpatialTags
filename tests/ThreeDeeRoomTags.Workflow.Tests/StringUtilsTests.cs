using System.Globalization;
using ThreeDeeRoomTags.Utilities;
using Xunit;

namespace ThreeDeeRoomTags.Workflow.Tests
{
    /// <summary>
    /// Characterization tests for the feet-and-inches parser.
    ///
    /// These pin what the parser does today, quirks included, so that any change to it shows
    /// up as a deliberate edit to an assertion rather than as a silent behavioral drift. The
    /// parser feeds a text height that is written to a family type inside a transaction, so
    /// "silent" here means wrong geometry in somebody's model.
    ///
    /// Every value is in inches.
    /// </summary>
    public class StringUtilsTests
    {
        /// <summary>
        /// Runs an assertion under a named culture and restores the previous one afterwards.
        /// The parser's behavior depends on the ambient culture — that dependency is the
        /// subject of several tests below, so it has to be controlled rather than inherited
        /// from whatever machine the suite runs on.
        /// </summary>
        private static void InCulture(string name, Action assertion)
        {
            var culture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                assertion();
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Theory]
        [InlineData("2' 6\"", 30)]
        [InlineData("2'6\"", 30)]
        [InlineData("2'", 24)]
        [InlineData("6\"", 6)]
        [InlineData("0' 1/2\"", 0.5)]
        [InlineData("1' 3/4\"", 12.75)]
        [InlineData("-3'", -36)]
        public void ParsesImperialNotation(string input, double expectedInches)
        {
            InCulture("en-US", () =>
                Assert.Equal(expectedInches, StringUtils.ParseStringFeetAndInches(input), 6));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("banana")]
        public void ReturnsZeroForUnparseableInput(string input)
        {
            InCulture("en-US", () =>
                Assert.Equal(0, StringUtils.ParseStringFeetAndInches(input)));
        }

        /// <summary>
        /// A bare number shorter than five digits matches nothing and comes back as zero,
        /// which the dialog surfaces as "unreadable". Documented because it is a reasonable
        /// thing for a user to type and the field's help text does not rule it out.
        /// </summary>
        [Theory]
        [InlineData("24")]
        [InlineData("6")]
        [InlineData("2")]
        public void RejectsBareNumbersUnderFiveDigits(string input)
        {
            InCulture("en-US", () =>
                Assert.Equal(0, StringUtils.ParseStringFeetAndInches(input)));
        }

        /// <summary>
        /// A bare number of five or more digits is silently read as packed
        /// feet/inches/sixteenths: "50603" is 5' 6 3/16". Undocumented anywhere in the UI.
        /// Pinned so that removing or documenting the format is a deliberate decision rather
        /// than an accident.
        /// </summary>
        [Theory]
        [InlineData("50603", 66.1875)]
        [InlineData("10000", 12)]
        public void ReadsFiveOrMoreDigitsAsPackedFeetInchesSixteenths(string input, double expectedInches)
        {
            InCulture("en-US", () =>
                Assert.Equal(expectedInches, StringUtils.ParseStringFeetAndInches(input), 6));
        }

        [Fact]
        public void ParsesDecimalFeetOnADotDecimalCulture()
        {
            InCulture("en-US", () =>
                Assert.Equal(18, StringUtils.ParseStringFeetAndInches("1.5'"), 6));
        }

        /// <summary>
        /// DEFECT, pinned deliberately (finding TAG-06).
        ///
        /// The feet group is converted with Convert.ToDouble, which follows the ambient
        /// culture and permits group separators. On a culture that groups with '.', "1.5"
        /// parses as fifteen — so a German user asking for 1.5 feet of text gets 15 feet,
        /// silently, persisted to their settings and written to the family type.
        ///
        /// This assertion is expected to change to 18 when the parser is made
        /// culture-invariant. It exists so that the fix has a visible before and after.
        /// </summary>
        [Fact]
        public void DecimalFeetAreMisparsedOnACommaDecimalCulture()
        {
            InCulture("de-DE", () =>
                Assert.Equal(180, StringUtils.ParseStringFeetAndInches("1.5'"), 6));
        }

        /// <summary>
        /// Whole feet and plain inches go through digit-only groups, so they are unaffected
        /// by the culture defect above. Worth pinning: it bounds the blast radius of TAG-06
        /// to decimal-feet input specifically.
        /// </summary>
        [Theory]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        public void WholeFeetAndInchesAreCultureIndependent(string culture)
        {
            InCulture(culture, () =>
            {
                Assert.Equal(30, StringUtils.ParseStringFeetAndInches("2' 6\""), 6);
                Assert.Equal(6, StringUtils.ParseStringFeetAndInches("6\""), 6);
                Assert.Equal(0.5, StringUtils.ParseStringFeetAndInches("0' 1/2\""), 6);
            });
        }
    }
}
