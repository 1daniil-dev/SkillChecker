namespace SkillChecker.Common.Models
{
    public class Question
    {
        private string _text;
        private List<string> _options;
        private int _correctAnswerIndex;
        private List<int> _correctAnswerIndices;
        private string _type;

        public string Text { get => _text; set => _text = value; }
        public List<string> Options { get => _options; set => _options = value; }
        public int CorrectAnswerIndex { get => _correctAnswerIndex; set => _correctAnswerIndex = value; }
        public List<int> CorrectAnswerIndices { get => _correctAnswerIndices; set => _correctAnswerIndices = value; }
        public string Type { get => _type; set => _type = value; }

        public Question()
        {
            _text = "";
            _options = new List<string>();
            _correctAnswerIndex = 0;
            _correctAnswerIndices = new List<int>();
            _type = "Single";
        }
    }
}
