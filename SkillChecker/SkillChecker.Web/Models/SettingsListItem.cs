namespace SkillChecker.Web.Models
{
    public class SettingsListItem
    {
        public string TestName { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string DisplayTime { get; set; } = "";
        public int TimeMinutes { get; set; }
        public bool Visible { get; set; } = true;
    }
}
