using System.Collections.Generic;
using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;

namespace SkillChecker.Tests
{
    public class ProtocolHelperTests
    {
        [Fact]
        public void BuildMessage_CommandOnly_NoTerminator()
        {
            string result = ProtocolHelper.BuildMessage("START");
            Assert.Equal("START", result);
        }

        [Fact]
        public void BuildMessage_WithParts_JoinedBySeparator()
        {
            string result = ProtocolHelper.BuildMessage("ANSWER", "1", "2", "3");
            Assert.Equal("ANSWER|1|2|3", result);
        }

        [Fact]
        public void ParseMessage_TrimsTrailingNewline_BackwardsCompatible()
        {
            string[] parts = ProtocolHelper.ParseMessage("OK\n");
            Assert.Single(parts);
            Assert.Equal("OK", parts[0]);
        }

        [Fact]
        public void ParseMessage_NoTerminator_StillSplits()
        {
            string[] parts = ProtocolHelper.ParseMessage("ANSWER|1|hello");
            Assert.Equal(3, parts.Length);
            Assert.Equal("ANSWER", parts[0]);
            Assert.Equal("1", parts[1]);
            Assert.Equal("hello", parts[2]);
        }

        [Fact]
        public void EncodeTextAnswer_PlainText_PrefixedWithT()
        {
            string result = ProtocolHelper.EncodeTextAnswer("hello");
            Assert.Equal("T:hello", result);
        }

        [Fact]
        public void EncodeTextAnswer_NullText_ReturnsPrefixOnly()
        {
            string result = ProtocolHelper.EncodeTextAnswer(null!);
            Assert.Equal("T:", result);
        }

        [Fact]
        public void EncodeTextAnswer_SpecialChars_AreUrlEncoded()
        {
            string result = ProtocolHelper.EncodeTextAnswer("hello world");
            Assert.Equal("T:hello%20world", result);
        }

        [Fact]
        public void DecodeTextAnswer_ValidPrefix_DecodesBack()
        {
            string result = ProtocolHelper.DecodeTextAnswer("T:hello%20world");
            Assert.Equal("hello world", result);
        }

        [Fact]
        public void DecodeTextAnswer_NoPrefix_ReturnsEmpty()
        {
            string result = ProtocolHelper.DecodeTextAnswer("hello");
            Assert.Equal("", result);
        }

        [Fact]
        public void DecodeTextAnswer_NullInput_ReturnsEmpty()
        {
            string result = ProtocolHelper.DecodeTextAnswer(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void EncodeAndDecodeTextAnswer_RoundTrip_Equal()
        {
            string original = "привет, мир! 100%";
            string encoded = ProtocolHelper.EncodeTextAnswer(original);
            string decoded = ProtocolHelper.DecodeTextAnswer(encoded);
            Assert.Equal(original, decoded);
        }

        [Fact]
        public void IsEncodedTextAnswer_WithPrefix_ReturnsTrue()
        {
            bool result = ProtocolHelper.IsEncodedTextAnswer("T:hello");
            Assert.True(result);
        }

        [Fact]
        public void IsEncodedTextAnswer_WithoutPrefix_ReturnsFalse()
        {
            bool result = ProtocolHelper.IsEncodedTextAnswer("hello");
            Assert.False(result);
        }

        [Fact]
        public void EncodeAcceptableAnswers_MultipleItems_JoinedByTilde()
        {
            List<string> answers = new List<string>();
            answers.Add("const");
            answers.Add("константа");
            string result = ProtocolHelper.EncodeAcceptableAnswers(answers);
            Assert.Equal("T:const~%D0%BA%D0%BE%D0%BD%D1%81%D1%82%D0%B0%D0%BD%D1%82%D0%B0", result);
        }

        [Fact]
        public void EncodeAcceptableAnswers_NullList_ReturnsPrefixOnly()
        {
            string result = ProtocolHelper.EncodeAcceptableAnswers(null!);
            Assert.Equal("T:", result);
        }

        [Fact]
        public void DecodeAcceptableAnswers_ValidEncoded_ReturnsList()
        {
            List<string> source = new List<string>();
            source.Add("hello world");
            source.Add("два");
            string encoded = ProtocolHelper.EncodeAcceptableAnswers(source);
            List<string> result = ProtocolHelper.DecodeAcceptableAnswers(encoded);
            Assert.Equal(2, result.Count);
            Assert.Equal("hello world", result[0]);
            Assert.Equal("два", result[1]);
        }

        [Fact]
        public void SerializeQuestionsWithoutAnswers_StripsCorrectAnswers()
        {
            Question q = new Question();
            q.Text = "Question one";
            q.Options = new List<string>();
            q.Options.Add("option1");
            q.Options.Add("option2");
            q.CorrectAnswerIndex = 1;
            q.AcceptableAnswers = new List<string>();
            q.AcceptableAnswers.Add("secret-answer");
            q.Type = "Single";
            List<Question> questions = new List<Question>();
            questions.Add(q);

            string json = ProtocolHelper.SerializeQuestionsWithoutAnswers(questions);

            Assert.Contains("Question one", json);
            Assert.Contains("option1", json);
            Assert.Contains("option2", json);
            Assert.DoesNotContain("CorrectAnswerIndex", json);
            Assert.DoesNotContain("AcceptableAnswers", json);
            Assert.DoesNotContain("secret-answer", json);
        }

        [Fact]
        public void DeserializeQuestions_RoundTripsFromQuestionJson()
        {
            string json = "[{\"Text\":\"Q1\",\"Options\":[\"a\",\"b\"],\"CorrectAnswerIndex\":1,\"Type\":\"Single\"}]";
            List<Question> result = ProtocolHelper.DeserializeQuestions(json);
            Assert.Single(result);
            Assert.Equal("Q1", result[0].Text);
            Assert.Equal(2, result[0].Options.Count);
            Assert.Equal("a", result[0].Options[0]);
            Assert.Equal("b", result[0].Options[1]);
            Assert.Equal(1, result[0].CorrectAnswerIndex);
            Assert.Equal("Single", result[0].Type);
        }
    }
}
