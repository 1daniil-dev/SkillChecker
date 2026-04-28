namespace SkillChecker.Common.Models
{
    public class TestSettings
    {
        private DateTime? _startTime;
        private int _timeMinutes;

        public DateTime? StartTime { get => _startTime; set => _startTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }

        public TestSettings()
        {
            _startTime = null;
            _timeMinutes = 0;
        }
    }
}
