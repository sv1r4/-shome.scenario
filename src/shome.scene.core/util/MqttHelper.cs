using System;
using System.Text.RegularExpressions;

namespace shome.scene.core.util
{
    public static class MqttHelper
    {
        public static bool IsSubscribed(string patternTopic, string actualTopic)
        {
            if (patternTopic.Equals(actualTopic, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            var rPattern = $"^{patternTopic.Replace("#", ".*").Replace("+", "[\\d,a-z]+")}$";
            var regex = new Regex(rPattern);
            return regex.IsMatch(actualTopic);
        }
    }
}
