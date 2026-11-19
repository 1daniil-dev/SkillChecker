using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkillChecker.Teacher.Models
{
    public class ExportGroupItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public string Group { get; set; } = "";
        public string DisplayText { get; set; } = "";
        public int Count { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
