namespace SkillChecker.Common.Models
{
    public class Question
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectAnswerIndex { get; set; }
    }
}
