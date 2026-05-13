using SkillChecker.Common.Models;

namespace SkillChecker.Tests
{
    public class NormalizeTextTests
    {
        [Fact]
        public void NullInput_ReturnsEmpty()
        {
            string result = AnswerChecker.NormalizeText(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void EmptyString_ReturnsEmpty()
        {
            string result = AnswerChecker.NormalizeText("");
            Assert.Equal("", result);
        }

        [Fact]
        public void OnlyWhitespace_ReturnsEmpty()
        {
            string result = AnswerChecker.NormalizeText("   \t\n  ");
            Assert.Equal("", result);
        }

        [Fact]
        public void SimpleWord_ReturnsLowercase()
        {
            string result = AnswerChecker.NormalizeText("Hello");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void LeadingAndTrailingSpaces_AreTrimmed()
        {
            string result = AnswerChecker.NormalizeText("  const  ");
            Assert.Equal("const", result);
        }

        [Fact]
        public void UppercaseInput_ReturnsLowercase()
        {
            string result = AnswerChecker.NormalizeText("CONST");
            Assert.Equal("const", result);
        }

        [Fact]
        public void MixedCaseInput_ReturnsLowercase()
        {
            string result = AnswerChecker.NormalizeText("Const");
            Assert.Equal("const", result);
        }

        [Fact]
        public void LowercaseYo_BecomesE()
        {
            string result = AnswerChecker.NormalizeText("ёлка");
            Assert.Equal("елка", result);
        }

        [Fact]
        public void UppercaseYo_BecomesE()
        {
            string result = AnswerChecker.NormalizeText("ЁЛКА");
            Assert.Equal("елка", result);
        }

        [Fact]
        public void MultipleSpaces_AreCollapsed()
        {
            string result = AnswerChecker.NormalizeText("привет    мир");
            Assert.Equal("привет мир", result);
        }

        [Fact]
        public void TabsAndNewlines_AreCollapsedToSingleSpace()
        {
            string result = AnswerChecker.NormalizeText("привет\t\tмир\nответ");
            Assert.Equal("привет мир ответ", result);
        }

        [Fact]
        public void DifferentInputsButSameMeaning_AreEqual()
        {
            string a = AnswerChecker.NormalizeText("  ЁЛКА  ");
            string b = AnswerChecker.NormalizeText("елка");
            Assert.Equal(a, b);
        }
    }
}
