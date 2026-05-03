namespace SkillChecker.ViewModels
{
    public class TestCardItem
    {
        private string _name;
        private bool _isScheduled;
        private string _scheduleTime;
        private int _timeMinutes;

        public string Name { get => _name; set => _name = value; }
        public bool IsScheduled { get => _isScheduled; set => _isScheduled = value; }
        public string ScheduleTime { get => _scheduleTime; set => _scheduleTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }

        public TestCardItem()
        {
            _name = "";
            _isScheduled = false;
            _scheduleTime = "";
            _timeMinutes = 0;
        }
    }
}
