namespace SkillChecker.Models
{
    public class ScheduledTest
    {
        public string Name { get; set; } = "";
        public DateTime ScheduledTime { get; set; } = DateTime.Now;
        public int TimeMinutes { get; set; }
    }
}
