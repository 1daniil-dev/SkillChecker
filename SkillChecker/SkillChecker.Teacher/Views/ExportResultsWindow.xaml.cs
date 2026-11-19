using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using SkillChecker.Teacher.Models;

namespace SkillChecker.Teacher.Views
{
    public partial class ExportResultsWindow : Window
    {
        public ObservableCollection<ExportGroupItem> Groups { get; } = new ObservableCollection<ExportGroupItem>();

        public ExportResultsWindow(List<ResultRow> results)
        {
            InitializeComponent();
            GroupsList.ItemsSource = Groups;
            BuildGroups(results);
            for (int i = 0; i < Groups.Count; i++)
            {
                Groups[i].PropertyChanged += Item_PropertyChanged;
            }
            UpdateCount();
        }

        public List<string> SelectedGroups
        {
            get
            {
                List<string> groups = new List<string>();
                for (int i = 0; i < Groups.Count; i++)
                {
                    if (Groups[i].IsSelected)
                    {
                        groups.Add(Groups[i].Group);
                    }
                }
                return groups;
            }
        }

        private void BuildGroups(List<ResultRow> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                string group = results[i].Group;
                ExportGroupItem? item = null;
                for (int j = 0; j < Groups.Count; j++)
                {
                    if (Groups[j].Group == group)
                    {
                        item = Groups[j];
                        break;
                    }
                }
                if (item == null)
                {
                    item = new ExportGroupItem();
                    item.Group = group;
                    Groups.Add(item);
                }
                item.Count++;
            }

            for (int i = 0; i < Groups.Count; i++)
            {
                string label = Groups[i].Group.Length > 0 ? Groups[i].Group : "(без группы)";
                Groups[i].DisplayText = label + " (" + Groups[i].Count + ")";
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateCount();
            }
        }

        private void UpdateCount()
        {
            int selectedGroups = 0;
            int selectedResults = 0;
            for (int i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].IsSelected)
                {
                    selectedGroups++;
                    selectedResults += Groups[i].Count;
                }
            }
            CountText.Text = "Выбрано групп: " + selectedGroups + " · результатов: " + selectedResults;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                Groups[i].IsSelected = true;
            }
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                Groups[i].IsSelected = false;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGroups.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну группу", "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
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
