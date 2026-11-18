namespace SkillChecker.Teacher.Models
{
    public class AnswerRow
    {
        public int Number { get; set; }
        public string QuestionText { get; set; } = "";
        public string YourAnswer { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }

        public string Header { get => Number + ". " + QuestionText; }
        public string YourAnswerText { get => "Ответ: " + YourAnswer; }
        public string CorrectAnswerText { get => "Правильный: " + CorrectAnswer; }
    }
}
