namespace SkillChecker.Web.Models
{
    public class TestListItem
    {
        private string _name;
        private int _questionCount;
        private bool _visible;
        private bool _hasSettings;
        private string _displayTime;
        private int _timeMinutes;

        public string Name { get => _name; set => _name = value; }
        public int QuestionCount { get => _questionCount; set => _questionCount = value; }
        public bool Visible { get => _visible; set => _visible = value; }
        public bool HasSettings { get => _hasSettings; set => _hasSettings = value; }
        public string DisplayTime { get => _displayTime; set => _displayTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }

        public TestListItem()
        {
            _name = "";
            _questionCount = 0;
            _visible = true;
            _hasSettings = false;
            _displayTime = "";
            _timeMinutes = 0;
        }
    }
}
