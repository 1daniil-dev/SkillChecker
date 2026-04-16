using System.Windows;
using SkillChecker.ViewModels;

namespace SkillChecker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
