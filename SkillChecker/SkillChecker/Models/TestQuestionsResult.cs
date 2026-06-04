using SkillChecker.Common.Models;

namespace SkillChecker.Models
{
    public class TestQuestionsResult
    {
        public List<Question> Questions { get; set; } = new List<Question>();
        public int TimeMinutes { get; set; }
        public bool IsWaiting { get; set; }
        public DateTime WaitTime { get; set; } = DateTime.Now;
    }
}
