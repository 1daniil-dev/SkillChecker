using System.Windows;
using SkillChecker.Teacher.ViewModels;
using SkillChecker.Teacher.Views;

namespace SkillChecker.Teacher
{
    public partial class MainWindow : Window
    {
        private TeacherViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new TeacherViewModel();
            DataContext = _viewModel;
            _viewModel.OpenEditorRequested += OpenEditor;
        }

        private void OpenEditor(string? testName)
        {
            TestEditorWindow window = new TestEditorWindow(_viewModel.TestsFolderPath, testName);
            window.Owner = this;
            window.ShowDialog();
            _viewModel.RefreshAfterEditor();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Stop();
            base.OnClosed(e);
        }
    }
}
