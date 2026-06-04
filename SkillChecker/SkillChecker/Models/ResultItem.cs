namespace SkillChecker.ViewModels
{
    public class ResultItem
    {
        public string Number { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public string SelectedAnswer { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
