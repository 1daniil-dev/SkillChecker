namespace SkillChecker.Common.Models
{
    public class StudentAnswer
    {
        public string QuestionText { get; set; } = "";
        public int SelectedIndex { get; set; } = -1;
        public int CorrectIndex { get; set; }
        public bool IsCorrect { get; set; }
        public string QuestionType { get; set; } = QuestionTypes.Single;
        public List<int> SelectedIndices { get; set; } = new List<int>();
        public string TextAnswer { get; set; } = "";
        public List<string> AcceptableAnswers { get; set; } = new List<string>();
    }

    public class TestResult
    {
        public string StudentName { get; set; } = "";
        public string Group { get; set; } = "";
        public string TestName { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double Score { get; set; }
        public List<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
    }
}
