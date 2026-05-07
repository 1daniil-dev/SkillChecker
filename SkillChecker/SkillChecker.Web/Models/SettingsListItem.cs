namespace SkillChecker.Web.Models
{
    public class SettingsListItem
    {
        private string _testName;
        private string _startTime;
        private string _displayTime;
        private int _timeMinutes;
        private bool _visible;

        public string TestName { get => _testName; set => _testName = value; }
        public string StartTime { get => _startTime; set => _startTime = value; }
        public string DisplayTime { get => _displayTime; set => _displayTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }
        public bool Visible { get => _visible; set => _visible = value; }

        public SettingsListItem()
        {
            _testName = "";
            _startTime = "";
            _displayTime = "";
            _timeMinutes = 0;
            _visible = true;
        }
    }
}
