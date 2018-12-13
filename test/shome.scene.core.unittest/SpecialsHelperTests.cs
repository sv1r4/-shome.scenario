using shome.scene.core.util;
using Xunit;

namespace shome.scene.core.unittest
{
    public class SpecialsHelperTests
    {
        [Theory]
        [InlineData("test", "test", true)]
        [InlineData("тест", "тест", true)]
        [InlineData("вкл*св*", "включи свет пожалуйста", true)]
        [InlineData("вкл*св*", "  включи свет  ", true)]
        [InlineData("вкл*св*", "включи плиз свет", true)]
        [InlineData("*вкл* св*", "будь добр олег включи свет", true)]
        [InlineData("вкл* св*", "включи  свет пожалуйста", true)]
        [InlineData("test", "test2", false)]
        [InlineData("тест", "тес", false)]
        [InlineData("вкл*св", "включи свет пожалуйста", false)]
        [InlineData("вкл* св*", "включисвет", false)]
        [InlineData("вкл* св*", "выключи свет", false)]
        [InlineData("вкл* св*", "будь добр олег включи свет", false)]
        public void IsSimpleMatchTests(string pattern, string value, bool expected)
        {
            Assert.Equal(expected, SpecialsHelper.IsSimpleMatch(pattern, value));
        }
    }
}
