namespace SkillChecker.Teacher.Models
{
    public class TestInfo
    {
        public string Name { get; set; } = "";
        public int QuestionCount { get; set; }

        public string QuestionCountText { get => "Вопросов: " + QuestionCount; }
    }
}
