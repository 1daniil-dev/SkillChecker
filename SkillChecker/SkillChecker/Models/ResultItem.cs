namespace SkillChecker.ViewModels
{
    public class ResultItem
    {
        private string _number;
        private string _questionText;
        private string _selectedAnswer;
        private string _correctAnswer;
        private bool _isCorrect;

        public string Number { get => _number; set => _number = value; }
        public string QuestionText { get => _questionText; set => _questionText = value; }
        public string SelectedAnswer { get => _selectedAnswer; set => _selectedAnswer = value; }
        public string CorrectAnswer { get => _correctAnswer; set => _correctAnswer = value; }
        public bool IsCorrect { get => _isCorrect; set => _isCorrect = value; }

        public ResultItem()
        {
            _number = "";
            _questionText = "";
            _selectedAnswer = "";
            _correctAnswer = "";
            _isCorrect = false;
        }
    }
}
