namespace SkillChecker.ViewModels
{
    public class ReviewItem
    {
        private string _number;
        private string _questionText;
        private bool _isAnswered;
        private int _index;
        private string _accessibilityName;

        public string Number { get => _number; set => _number = value; }
        public string QuestionText { get => _questionText; set => _questionText = value; }
        public bool IsAnswered { get => _isAnswered; set => _isAnswered = value; }
        public int Index { get => _index; set => _index = value; }
        public string ReviewAccessibilityName { get => _accessibilityName; set => _accessibilityName = value; }

        public ReviewItem()
        {
            _number = "";
            _questionText = "";
            _isAnswered = false;
            _index = 0;
            _accessibilityName = "";
        }
    }
}
