namespace SkillChecker.Common.Models
{
    public class QuestionView
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public string Type { get; set; } = QuestionTypes.Single;
    }
}
