namespace SkillChecker.Web.Models
{
    public class TestListItem
    {
        public string Name { get; set; } = "";
        public int QuestionCount { get; set; }
        public bool Visible { get; set; } = true;
        public bool HasSettings { get; set; }
        public string DisplayTime { get; set; } = "";
        public int TimeMinutes { get; set; }
    }
}
