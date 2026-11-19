using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SkillChecker.Teacher.ViewModels;
using SkillChecker.Teacher.Views;

namespace SkillChecker.Teacher
{
    public partial class MainWindow : Window
    {
        private TeacherViewModel _viewModel;
        private GridLength _detailsHeight = new GridLength(2, GridUnitType.Star);

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new TeacherViewModel();
            DataContext = _viewModel;
            _viewModel.OpenEditorRequested += OpenEditor;
            _viewModel.OpenSettingsRequested += OpenSettings;
            _viewModel.OpenPreviewRequested += OpenPreview;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            NavList.SelectedIndex = 0;
            UpdatePanels();
            UpdateDetailsVisibility();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedResult")
            {
                UpdateDetailsVisibility();
            }
        }

        private void UpdateDetailsVisibility()
        {
            bool hasSelection = _viewModel.SelectedResult != null;
            if (hasSelection)
            {
                if (DetailsRow.Height.Value == 0)
                {
                    DetailsRow.Height = _detailsHeight;
                }
                DetailsPanel.Visibility = Visibility.Visible;
                DetailsSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                if (DetailsRow.Height.Value > 0)
                {
                    _detailsHeight = DetailsRow.Height;
                }
                DetailsRow.Height = new GridLength(0);
                DetailsPanel.Visibility = Visibility.Collapsed;
                DetailsSplitter.Visibility = Visibility.Collapsed;
            }
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

        private void OpenSettings(string testName)
        {
            TestSettingsWindow window = new TestSettingsWindow(_viewModel, testName);
            window.Owner = this;
            window.ShowDialog();
        }

        private void OpenPreview(string testName)
        {
            TestPreviewWindow window = new TestPreviewWindow(_viewModel.TestsFolderPath, testName);
            window.Owner = this;
            window.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Stop();
            base.OnClosed(e);
        }
    }
}
