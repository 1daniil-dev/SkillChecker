using System.Collections.Generic;
using SkillChecker.Common.Models;

namespace SkillChecker.Tests
{
    public class CheckTextAnswerTests
    {
        [Fact]
        public void ExactMatch_ReturnsTrue()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("const", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void DifferentCase_ReturnsTrue()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("CONST", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void ExtraSpacesAroundAnswer_ReturnsTrue()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("   const   ", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void YoInAnswerMatchesEInAcceptable_ReturnsTrue()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("елка");
            bool result = AnswerChecker.CheckTextAnswer("ёлка", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void AcceptableWithMixedCaseAndSpaces_StillMatches()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("  CONST  ");
            bool result = AnswerChecker.CheckTextAnswer("const", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void OneOfManyAcceptable_ReturnsTrue()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("два");
            acceptable.Add("2");
            acceptable.Add("two");
            bool result = AnswerChecker.CheckTextAnswer("2", acceptable);
            Assert.True(result);
        }

        [Fact]
        public void NoneOfAcceptableMatches_ReturnsFalse()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            acceptable.Add("константа");
            bool result = AnswerChecker.CheckTextAnswer("var", acceptable);
            Assert.False(result);
        }

        [Fact]
        public void TypoInAnswer_ReturnsFalse()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("konst", acceptable);
            Assert.False(result);
        }

        [Fact]
        public void EmptyTypedAnswer_ReturnsFalse()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("", acceptable);
            Assert.False(result);
        }

        [Fact]
        public void WhitespaceOnlyAnswer_ReturnsFalse()
        {
            List<string> acceptable = new List<string>();
            acceptable.Add("const");
            bool result = AnswerChecker.CheckTextAnswer("   ", acceptable);
            Assert.False(result);
        }

        [Fact]
        public void NullAcceptableList_ReturnsFalse()
        {
            bool result = AnswerChecker.CheckTextAnswer("const", null!);
            Assert.False(result);
        }

        [Fact]
        public void EmptyAcceptableList_ReturnsFalse()
        {
            List<string> acceptable = new List<string>();
            bool result = AnswerChecker.CheckTextAnswer("const", acceptable);
            Assert.False(result);
        }
    }
}
