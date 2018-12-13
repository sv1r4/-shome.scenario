using shome.scene.core.util;
using Xunit;

namespace shome.scene.core.unittest
{
    public class MqttHelperTests
    {
        [Theory]
        [InlineData("a/b", "a/b", true)]
        [InlineData("a/+", "a/b", true)]
        [InlineData("+/+", "a/b", true)]
        [InlineData("+/b", "a/b", true)]
        [InlineData("#/b", "a/b", true)]
        [InlineData("a/#", "a/b", true)]
        [InlineData("#", "a/b", true)]
        [InlineData("#", "a/b/c", true)]
        [InlineData("a/#", "a/b/c", true)]
        [InlineData("a/#", "a/c", true)]
        [InlineData("a/+/c", "a/b/c", true)]

        [InlineData("a/b", "a/b/c", false)]
        [InlineData("b/a", "a/b", false)]
        [InlineData("b/+", "a/b", false)]
        [InlineData("+/+/+", "a/b", false)]
        [InlineData("+/a", "a/b", false)]
        [InlineData("#/a", "a/b", false)]
        [InlineData("b/#", "a/b", false)]
        [InlineData("a/#", "c", false)]
        [InlineData("a/#", "b/c", false)]
        [InlineData("a/+/c", "a/c", false)]
        [InlineData("+", "a/b", false)]
        [InlineData("a/+", "a/b/c", false)]
        public void IsSubscribedTests(string patternTopic, string topic, bool expected)
        {
            Assert.Equal(expected, MqttHelper.IsSubscribed(patternTopic, topic));
        }
    }
}
