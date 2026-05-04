namespace SkillChecker.Common.Models
{
    public class QuestionView
    {
        private string _text;
        private List<string> _options;
        private string _type;

        public string Text { get => _text; set => _text = value; }
        public List<string> Options { get => _options; set => _options = value; }
        public string Type { get => _type; set => _type = value; }

        public QuestionView()
        {
            _text = "";
            _options = new List<string>();
            _type = "Single";
        }
    }
}
