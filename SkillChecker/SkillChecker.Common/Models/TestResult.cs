namespace SkillChecker.Common.Models
{
    public class StudentAnswer
    {
        private string _questionText;
        private int _selectedIndex;
        private int _correctIndex;
        private bool _isCorrect;
        private string _questionType;
        private List<int> _selectedIndices;

        public string QuestionText { get => _questionText; set => _questionText = value; }
        public int SelectedIndex { get => _selectedIndex; set => _selectedIndex = value; }
        public int CorrectIndex { get => _correctIndex; set => _correctIndex = value; }
        public bool IsCorrect { get => _isCorrect; set => _isCorrect = value; }
        public string QuestionType { get => _questionType; set => _questionType = value; }
        public List<int> SelectedIndices { get => _selectedIndices; set => _selectedIndices = value; }

        public StudentAnswer()
        {
            _questionText = "";
            _selectedIndex = -1;
            _correctIndex = 0;
            _isCorrect = false;
            _questionType = "Single";
            _selectedIndices = new List<int>();
        }
    }

    public class TestResult
    {
        private string _studentName;
        private string _group;
        private string _testName;
        private DateTime _date;
        private int _totalQuestions;
        private int _correctAnswers;
        private double _score;
        private List<StudentAnswer> _answers;

        public string StudentName { get => _studentName; set => _studentName = value; }
        public string Group { get => _group; set => _group = value; }
        public string TestName { get => _testName; set => _testName = value; }
        public DateTime Date { get => _date; set => _date = value; }
        public int TotalQuestions { get => _totalQuestions; set => _totalQuestions = value; }
        public int CorrectAnswers { get => _correctAnswers; set => _correctAnswers = value; }
        public double Score { get => _score; set => _score = value; }
        public List<StudentAnswer> Answers { get => _answers; set => _answers = value; }

        public TestResult()
        {
            _studentName = "";
            _group = "";
            _testName = "";
            _date = DateTime.Now;
            _totalQuestions = 0;
            _correctAnswers = 0;
            _score = 0;
            _answers = new List<StudentAnswer>();
        }
    }
}
