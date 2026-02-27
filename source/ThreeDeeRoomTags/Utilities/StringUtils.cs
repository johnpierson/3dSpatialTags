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
            double feet = m.Groups["feet"].Success && double.TryParse(m.Groups["feet"].Value, out double parsedFeet) ? parsedFeet : 0;
            int inch = m.Groups["inch"].Success && int.TryParse(m.Groups["inch"].Value, out int parsedInch) ? parsedInch : 0;
            int sixt = m.Groups["sixt"].Success && int.TryParse(m.Groups["sixt"].Value, out int parsedSixt) ? parsedSixt : 0;
            int numer = m.Groups["numer"].Success && int.TryParse(m.Groups["numer"].Value, out int parsedNumer) ? parsedNumer : 0;
            int denom = m.Groups["denom"].Success && int.TryParse(m.Groups["denom"].Value, out int parsedDenom) && parsedDenom != 0 ? parsedDenom : 1;
            return sign * (feet * 12 + inch + sixt / 16.0 + numer / (double)denom);
        }
    }
}
