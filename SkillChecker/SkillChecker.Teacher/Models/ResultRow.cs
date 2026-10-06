namespace SkillChecker.Teacher.Models
{
    public class ResultRow
    {
        public string StudentName { get; set; } = "";
        public string Group { get; set; } = "";
        public string TestName { get; set; } = "";
        public DateTime Date { get; set; }
        public double Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }

        public string DateText { get => Date.ToString("dd.MM HH:mm"); }
        public string ScoreText { get => Score.ToString("0.#") + "%"; }
        public string CorrectText { get => CorrectAnswers + " / " + TotalQuestions; }

        public int ScoreLevel
        {
            get
            {
                if (Score >= 70)
                {
                    return 2;
                }
                if (Score >= 40)
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}
