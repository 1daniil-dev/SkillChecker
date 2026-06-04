namespace SkillChecker.Web.Models
{
    public class SettingsRequest
    {
        public string TestName { get; set; } = "";
        public string StartTime { get; set; } = "";
        public int TimeMinutes { get; set; } = 0;
    }
}
