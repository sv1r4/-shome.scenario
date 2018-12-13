using shome.scene.core.util;
using Xunit;

namespace shome.scene.core.unittest
{
    public class SpecialsHelperTests
    {
        [Theory]
        [InlineData("test", "test", true)]
        [InlineData("����", "����", true)]
        [InlineData("���*��*", "������ ���� ����������", true)]
        [InlineData("���*��*", "  ������ ����  ", true)]
        [InlineData("���*��*", "������ ���� ����", true)]
        [InlineData("*���* ��*", "���� ���� ���� ������ ����", true)]
        [InlineData("���* ��*", "������  ���� ����������", true)]
        [InlineData("test", "test2", false)]
        [InlineData("����", "���", false)]
        [InlineData("���*��", "������ ���� ����������", false)]
        [InlineData("���* ��*", "����������", false)]
        [InlineData("���* ��*", "������� ����", false)]
        [InlineData("���* ��*", "���� ���� ���� ������ ����", false)]
        public void IsSimpleMatchTests(string pattern, string value, bool expected)
        {
            Assert.Equal(expected, SpecialsHelper.IsSimpleMatch(pattern, value));
        }
    }
}
