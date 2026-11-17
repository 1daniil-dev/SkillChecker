using System.Globalization;
using System.Windows;
using SkillChecker.Common.Models;
using SkillChecker.Teacher.ViewModels;

namespace SkillChecker.Teacher.Views
{
    public partial class TestSettingsWindow : Window
    {
        private TeacherViewModel _viewModel;
        private string _testName;

        public TestSettingsWindow(TeacherViewModel viewModel, string testName)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _testName = testName;
            TestNameText.Text = testName;
            LoadSettings();
        }

        private void LoadSettings()
        {
            TestSettings? settings = _viewModel.GetTestSettingsFor(_testName);
            if (settings == null)
            {
                return;
            }

            if (settings.StartTime != null)
            {
                StartDatePicker.SelectedDate = settings.StartTime.Value.Date;
                StartTimeBox.Text = settings.StartTime.Value.ToString("HH:mm");
            }
            LimitBox.Text = settings.TimeMinutes.ToString();
            VisibleBox.IsChecked = settings.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            DateTime? startTime = null;
            string timeText = StartTimeBox.Text.Trim();
            if (StartDatePicker.SelectedDate != null || timeText.Length > 0)
            {
                if (StartDatePicker.SelectedDate == null)
                {
                    ErrorText.Text = "Укажите дату начала теста";
                    return;
                }
                if (timeText.Length == 0)
                {
                    ErrorText.Text = "Укажите время начала (ЧЧ:мм)";
                    return;
                }

                DateTime time;
                if (!DateTime.TryParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                {
                    ErrorText.Text = "Время должно быть в формате ЧЧ:мм";
                    return;
                }
                startTime = StartDatePicker.SelectedDate.Value.Date.Add(time.TimeOfDay);
            }

            int minutes = 0;
            string limitText = LimitBox.Text.Trim();
            if (limitText.Length > 0)
            {
                if (!int.TryParse(limitText, out minutes) || minutes < 0)
                {
                    ErrorText.Text = "Лимит времени должен быть неотрицательным числом";
                    return;
                }
            }

            bool visible = VisibleBox.IsChecked == true;

            _viewModel.SaveSettingsForTest(_testName, startTime, minutes, visible);
            DialogResult = true;
            Close();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show("Удалить настройки теста «" + _testName + "»?", "SkillChecker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
            _viewModel.RemoveSettingsForTest(_testName);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
