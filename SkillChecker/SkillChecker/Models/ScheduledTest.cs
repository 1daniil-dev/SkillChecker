namespace SkillChecker.Models
{
    public class ScheduledTest
    {
        private string _name;
        private DateTime _scheduledTime;
        private int _timeMinutes;

        public string Name { get => _name; set => _name = value; }
        public DateTime ScheduledTime { get => _scheduledTime; set => _scheduledTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }

        public ScheduledTest()
        {
            _name = "";
            _scheduledTime = DateTime.Now;
            _timeMinutes = 0;
        }
    }
}
