using System.ComponentModel.DataAnnotations;

namespace SkillChecker.Data
{
    public class ResultEntity
    {
        private int _id;
        private string _studentName;
        private string _group;
        private string _testName;
        private DateTime _date;
        private int _totalQuestions;
        private int _correctAnswers;
        private double _score;
        private string _answersJson;
        private string _sourceFile;

        [Key]
        public int Id { get => _id; set => _id = value; }
        public string StudentName { get => _studentName; set => _studentName = value; }
        public string Group { get => _group; set => _group = value; }
        public string TestName { get => _testName; set => _testName = value; }
        public DateTime Date { get => _date; set => _date = value; }
        public int TotalQuestions { get => _totalQuestions; set => _totalQuestions = value; }
        public int CorrectAnswers { get => _correctAnswers; set => _correctAnswers = value; }
        public double Score { get => _score; set => _score = value; }
        public string AnswersJson { get => _answersJson; set => _answersJson = value; }
        public string SourceFile { get => _sourceFile; set => _sourceFile = value; }

        public ResultEntity()
        {
            _id = 0;
            _studentName = "";
            _group = "";
            _testName = "";
            _date = DateTime.Now;
            _totalQuestions = 0;
            _correctAnswers = 0;
            _score = 0;
            _answersJson = "[]";
            _sourceFile = "";
        }
    }
}
