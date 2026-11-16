using System.Windows;
using System.Windows.Controls;
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
            NavList.SelectedIndex = 0;
            UpdatePanels();
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanels();
        }

        private void UpdatePanels()
        {
            ListBoxItem? item = NavList.SelectedItem as ListBoxItem;
            string tag = item != null ? (item.Tag as string ?? "") : "";

            OverviewPanel.Visibility = tag == "overview" ? Visibility.Visible : Visibility.Collapsed;
            TestsPanel.Visibility = tag == "tests" ? Visibility.Visible : Visibility.Collapsed;
            ResultsPanel.Visibility = tag == "results" ? Visibility.Visible : Visibility.Collapsed;
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
