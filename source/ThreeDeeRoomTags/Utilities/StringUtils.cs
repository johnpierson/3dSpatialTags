using System.Globalization;
using System.Text.RegularExpressions;

namespace ThreeDeeRoomTags.Utilities
{
    internal class StringUtils
    {
        public static double ParseStringFeetAndInches(string inp)
        {
            string expr = "^\\s*(?<minus>-)?\\s*(((?<feet>\\d+)(?<inch>\\d{2})(?<sixt>\\d{2}))|((?<feet>[\\d.]+)')?[\\s-]*((?<inch>\\d+)?[\\s-]*((?<numer>\\d+)/(?<denom>\\d+))?\")?)\\s*$";
            Match m = new Regex(expr).Match(inp);
            if (!m.Success || inp.Trim() == "")
            {
                // maybe throw exception or set/return some failure indicator
                return 0; // here using return value zero as failure indicator
            }
            int sign = m.Groups["minus"].Success ? -1 : 1;

            // Parsed invariantly, not with Convert.ToDouble. Convert.ToDouble follows the
            // ambient culture and permits group separators, and .NET does not check the digit
            // grouping — so on a culture that groups with '.', "1.5" came back as fifteen. A
            // German user asking for 1.5 feet of text got 15, silently, written to the family
            // type and saved to their settings. The regex above only ever admits '.' as a
            // decimal point, so invariant parsing is the correct reading of what it matched.
            //
            // TryParse rather than Parse: input like "1.2.3" matches the regex but is not a
            // number, and returning the failure indicator puts it in front of the user as an
            // unreadable height instead of throwing into a swallowed catch.
            double feet = 0;
            if (m.Groups["feet"].Success &&
                !double.TryParse(m.Groups["feet"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out feet))
            {
                return 0;
            }

            int inch = m.Groups["inch"].Success ? Convert.ToInt32(m.Groups["inch"].Value) : 0;
            int sixt = m.Groups["sixt"].Success ? Convert.ToInt32(m.Groups["sixt"].Value) : 0;
            int numer = m.Groups["numer"].Success ? Convert.ToInt32(m.Groups["numer"].Value) : 0;
            int denom = m.Groups["denom"].Success ? Convert.ToInt32(m.Groups["denom"].Value) : 1;
            return sign * (feet * 12 + inch + sixt / 16.0 + numer / Convert.ToDouble(denom));
        }
    }
}
