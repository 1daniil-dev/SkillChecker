namespace SkillChecker.Teacher.Models
{
    public class TestInfo
    {
        public string Name { get; set; } = "";
        public int QuestionCount { get; set; }
        public string StatusText { get; set; } = "Доступен";
        public string DetailsText { get; set; } = "";
        public int StatusLevel { get; set; }

        public string QuestionCountText { get => "Вопросов: " + QuestionCount; }
    }
}
