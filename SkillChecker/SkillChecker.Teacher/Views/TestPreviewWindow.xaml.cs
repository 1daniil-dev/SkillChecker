using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using SkillChecker.Common.Models;
using SkillChecker.Teacher.Models;

namespace SkillChecker.Teacher.Views
{
    public partial class TestPreviewWindow : Window
    {
        private string _filePath;
        private List<Question> _questions = new List<Question>();

        public ObservableCollection<PreviewQuestion> Items { get; } = new ObservableCollection<PreviewQuestion>();

        public TestPreviewWindow(string testsFolder, string testName)
        {
            InitializeComponent();
            Title = "Предпросмотр — " + testName;
            TestNameText.Text = testName;
            _filePath = Path.Combine(testsFolder, testName + ".json");
            QuestionsList.ItemsSource = Items;
            LoadQuestions();
            BuildItems();
        }

        private void LoadQuestions()
        {
            if (!File.Exists(_filePath))
            {
                MessageBox.Show("Тест не найден: " + _filePath, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath, Encoding.UTF8);
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<Question>? list = JsonSerializer.Deserialize<List<Question>>(json, options);
                if (list != null)
                {
                    _questions = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось прочитать тест: " + ex.Message, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildItems()
        {
            bool showCorrect = ShowCorrectBox.IsChecked == true;
            Items.Clear();

            for (int i = 0; i < _questions.Count; i++)
            {
                Question question = _questions[i];
                PreviewQuestion item = new PreviewQuestion();
                item.Title = (i + 1) + ". " + question.Text;

                if (question.Type == QuestionTypes.Text)
                {
                    item.TypeHint = "Текстовый ввод";
                    if (showCorrect)
                    {
                        item.AcceptableText = "Допустимые ответы: " + string.Join(", ", question.AcceptableAnswers);
                        item.HasAcceptable = true;
                    }
                }
                else
                {
                    if (question.Type == QuestionTypes.Multiple)
                    {
                        item.TypeHint = "Множественный выбор";
                    }
                    else
                    {
                        item.TypeHint = "Одиночный выбор";
                    }

                    for (int j = 0; j < question.Options.Count; j++)
                    {
                        PreviewOption option = new PreviewOption();
                        char letter = (char)(1040 + j);
                        option.DisplayText = letter + ") " + question.Options[j];

                        if (showCorrect)
                        {
                            if (question.Type == QuestionTypes.Multiple)
                            {
                                option.IsCorrect = question.CorrectAnswerIndices.Contains(j);
                            }
                            else
                            {
                                option.IsCorrect = j == question.CorrectAnswerIndex;
                            }
                        }

                        item.Options.Add(option);
                    }
                }

                Items.Add(item);
            }
        }

        private void ShowCorrect_Changed(object sender, RoutedEventArgs e)
        {
            BuildItems();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
