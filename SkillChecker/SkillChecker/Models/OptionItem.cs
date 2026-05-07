namespace SkillChecker.ViewModels
{
    public class OptionItem
    {
        private int _index;
        private string _text;
        private bool _isSelected;

        public int Index { get => _index; set => _index = value; }
        public string Text { get => _text; set => _text = value; }
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }

        public OptionItem()
        {
            _index = 0;
            _text = "";
            _isSelected = false;
        }
    }
}
