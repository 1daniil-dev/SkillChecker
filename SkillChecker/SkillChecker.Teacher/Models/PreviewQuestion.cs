namespace SkillChecker.Teacher.Models
{
    public class PreviewOption
    {
        public string DisplayText { get; set; } = "";
        public bool IsCorrect { get; set; }
    }

    public class PreviewQuestion
    {
        public string Title { get; set; } = "";
        public string TypeHint { get; set; } = "";
        public List<PreviewOption> Options { get; set; } = new List<PreviewOption>();
        public string AcceptableText { get; set; } = "";
        public bool HasAcceptable { get; set; }
    }
}
