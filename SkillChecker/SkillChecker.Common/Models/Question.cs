namespace SkillChecker.Common.Models
{
    public class Question
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectAnswerIndex { get; set; }
        public List<int> CorrectAnswerIndices { get; set; } = new List<int>();
        public List<string> AcceptableAnswers { get; set; } = new List<string>();
        public string Type { get; set; } = QuestionTypes.Single;
    }
}
