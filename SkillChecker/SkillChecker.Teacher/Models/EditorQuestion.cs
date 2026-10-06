using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SkillChecker.Common.Models;

namespace SkillChecker.Teacher.Models
{
    public class EditorOption : INotifyPropertyChanged
    {
        private string _text = "";
        private bool _isCorrect;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set { _isCorrect = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class EditorQuestion : INotifyPropertyChanged
    {
        private string _text = "";
        private string _type = QuestionTypes.Single;
        private string _acceptableText = "";
        private int _number;

        public string GroupName { get; } = Guid.NewGuid().ToString("N");

        public ObservableCollection<EditorOption> Options { get; } = new ObservableCollection<EditorOption>();

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); OnPropertyChanged("DisplayText"); }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged("IsText");
                OnPropertyChanged("IsOptions");
                OnPropertyChanged("IsSingle");
                OnPropertyChanged("IsMultiple");
            }
        }

        public string AcceptableText
        {
            get => _acceptableText;
            set { _acceptableText = value; OnPropertyChanged(); }
        }

        public int Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); OnPropertyChanged("DisplayText"); }
        }

        public bool IsText { get => _type == QuestionTypes.Text; }
        public bool IsOptions { get => _type != QuestionTypes.Text; }
        public bool IsSingle { get => _type == QuestionTypes.Single; }
        public bool IsMultiple { get => _type == QuestionTypes.Multiple; }

        public string DisplayText
        {
            get
            {
                string preview = _text;
                if (preview.Length > 45)
                {
                    preview = preview.Substring(0, 45) + "...";
                }
                return _number + ". " + preview;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
