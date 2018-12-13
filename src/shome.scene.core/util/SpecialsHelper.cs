using System;
using System.Text.RegularExpressions;

namespace shome.scene.core.util
{
    public static class SpecialsHelper
    {
        public static bool IsSimpleMatch(string pattern, string value)
        {
            if (pattern.Equals(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            
            var rPattern = $@"^\s*{pattern.Replace("*", ".*").Replace(" ", @"\s+")}\s*$";
            var regex = new Regex(rPattern);
            return regex.IsMatch(value);
        }
    }
}
