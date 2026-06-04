namespace SkillChecker.ViewModels
{
    public class ReviewItem
    {
        public string Number { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public bool IsAnswered { get; set; }
        public int Index { get; set; }
        public string ReviewAccessibilityName { get; set; } = "";
    }
}
