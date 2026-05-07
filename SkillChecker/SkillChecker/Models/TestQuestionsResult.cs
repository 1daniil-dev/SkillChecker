using SkillChecker.Common.Models;

namespace SkillChecker.Services
{
    public class TestQuestionsResult
    {
        private List<Question> _questions;
        private int _timeMinutes;
        private bool _isWaiting;
        private DateTime _waitTime;

        public List<Question> Questions { get => _questions; set => _questions = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }
        public bool IsWaiting { get => _isWaiting; set => _isWaiting = value; }
        public DateTime WaitTime { get => _waitTime; set => _waitTime = value; }

        public TestQuestionsResult()
        {
            _questions = new List<Question>();
            _timeMinutes = 0;
            _isWaiting = false;
            _waitTime = DateTime.Now;
        }
    }
}
