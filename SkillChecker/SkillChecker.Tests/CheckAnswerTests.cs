using System.Collections.Generic;
using SkillChecker.Common.Models;

namespace SkillChecker.Tests
{
    public class CheckAnswerTests
    {
        [Fact]
        public void Single_CorrectIndex_ReturnsTrue()
        {
            List<int> selected = new List<int>();
            selected.Add(2);
            List<int> correct = new List<int>();
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Single");
            Assert.True(result);
        }

        [Fact]
        public void Single_WrongIndex_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            selected.Add(1);
            List<int> correct = new List<int>();
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Single");
            Assert.False(result);
        }

        [Fact]
        public void Single_NothingSelected_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            List<int> correct = new List<int>();
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Single");
            Assert.False(result);
        }

        [Fact]
        public void Single_UnknownQuestionType_TreatedAsSingle()
        {
            List<int> selected = new List<int>();
            selected.Add(0);
            List<int> correct = new List<int>();
            correct.Add(0);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Text");
            Assert.True(result);
        }

        [Fact]
        public void Multiple_SameElementsSameOrder_ReturnsTrue()
        {
            List<int> selected = new List<int>();
            selected.Add(0);
            selected.Add(1);
            selected.Add(2);
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.True(result);
        }

        [Fact]
        public void Multiple_SameElementsDifferentOrder_ReturnsTrue()
        {
            List<int> selected = new List<int>();
            selected.Add(2);
            selected.Add(0);
            selected.Add(1);
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.True(result);
        }

        [Fact]
        public void Multiple_TooFewSelected_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            selected.Add(0);
            selected.Add(1);
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.False(result);
        }

        [Fact]
        public void Multiple_TooManySelected_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            selected.Add(0);
            selected.Add(1);
            selected.Add(2);
            selected.Add(3);
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.False(result);
        }

        [Fact]
        public void Multiple_SameSizeButOneWrong_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            selected.Add(0);
            selected.Add(1);
            selected.Add(3);
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            correct.Add(2);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.False(result);
        }

        [Fact]
        public void Multiple_NothingSelected_ReturnsFalse()
        {
            List<int> selected = new List<int>();
            List<int> correct = new List<int>();
            correct.Add(0);
            correct.Add(1);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.False(result);
        }

        [Fact]
        public void Multiple_SingleCorrectElement_ReturnsTrue()
        {
            List<int> selected = new List<int>();
            selected.Add(5);
            List<int> correct = new List<int>();
            correct.Add(5);
            bool result = AnswerChecker.CheckAnswer(selected, correct, "Multiple");
            Assert.True(result);
        }
    }
}
