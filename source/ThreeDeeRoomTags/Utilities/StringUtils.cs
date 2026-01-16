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
            double feet = m.Groups["feet"].Success ? Convert.ToDouble(m.Groups["feet"].Value) : 0;
            int inch = m.Groups["inch"].Success ? Convert.ToInt32(m.Groups["inch"].Value) : 0;
            int sixt = m.Groups["sixt"].Success ? Convert.ToInt32(m.Groups["sixt"].Value) : 0;
            int numer = m.Groups["numer"].Success ? Convert.ToInt32(m.Groups["numer"].Value) : 0;
            int denom = m.Groups["denom"].Success ? Convert.ToInt32(m.Groups["denom"].Value) : 1;
            return sign * (feet * 12 + inch + sixt / 16.0 + numer / Convert.ToDouble(denom));
        }
    }
}
