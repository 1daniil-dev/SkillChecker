using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SkillChecker.Common.Models;
using SkillChecker.Teacher.Models;

namespace SkillChecker.Teacher.Views
{
    public partial class TestEditorWindow : Window
    {
        private string _testsFolder;
        private bool _isNew;

        public ObservableCollection<EditorQuestion> Questions { get; } = new ObservableCollection<EditorQuestion>();

        public TestEditorWindow(string testsFolder, string? testName)
        {
            InitializeComponent();
            _testsFolder = testsFolder;
            _isNew = testName == null;
            QuestionList.ItemsSource = Questions;

            if (_isNew)
            {
                Title = "Новый тест";
                AddQuestion();
            }
            else
            {
                Title = "Редактор теста — " + testName;
                NameBox.Text = testName;
                NameBox.IsReadOnly = true;
                LoadTest(testName!);
            }
        }

        private void LoadTest(string testName)
        {
            string filePath = Path.Combine(_testsFolder, testName + ".json");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Тест не найден: " + testName, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<Question>? list = JsonSerializer.Deserialize<List<Question>>(json, options);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Questions.Add(ToEditorQuestion(list[i]));
                    }
                }
                Renumber();
                if (Questions.Count > 0)
                {
                    QuestionList.SelectedItem = Questions[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось прочитать тест: " + ex.Message, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void AddQuestion()
        {
            EditorQuestion question = new EditorQuestion();
            question.Options.Add(new EditorOption());
            question.Options.Add(new EditorOption());
            Questions.Add(question);
            Renumber();
            QuestionList.SelectedItem = question;
        }

        private void Renumber()
        {
            for (int i = 0; i < Questions.Count; i++)
            {
                Questions[i].Number = i + 1;
            }
        }

        private void AddQuestion_Click(object sender, RoutedEventArgs e)
        {
            AddQuestion();
        }

        private void RemoveQuestion_Click(object sender, RoutedEventArgs e)
        {
            EditorQuestion? question = QuestionList.SelectedItem as EditorQuestion;
            if (question == null)
            {
                return;
            }
            Questions.Remove(question);
            Renumber();
        }

        private void AddOption_Click(object sender, RoutedEventArgs e)
        {
            EditorQuestion? question = QuestionList.SelectedItem as EditorQuestion;
            if (question == null)
            {
                return;
            }
            question.Options.Add(new EditorOption());
        }

        private void RemoveOption_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }
            EditorOption? option = button.DataContext as EditorOption;
            EditorQuestion? question = QuestionList.SelectedItem as EditorQuestion;
            if (option == null || question == null)
            {
                return;
            }
            question.Options.Remove(option);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            string name = NameBox.Text.Trim();
            if (name.Length == 0)
            {
                ErrorText.Text = "Введите название теста";
                return;
            }
            if (!IsValidName(name))
            {
                ErrorText.Text = "Название содержит недопустимые символы";
                return;
            }
            if (Questions.Count == 0)
            {
                ErrorText.Text = "Добавьте хотя бы один вопрос";
                return;
            }

            List<Question> questions = new List<Question>();
            for (int i = 0; i < Questions.Count; i++)
            {
                string error = ValidateQuestion(Questions[i], i + 1);
                if (error.Length > 0)
                {
                    ErrorText.Text = error;
                    return;
                }
                questions.Add(ToQuestion(Questions[i]));
            }

            string filePath = Path.Combine(_testsFolder, name + ".json");
            if (_isNew && File.Exists(filePath))
            {
                MessageBoxResult answer = MessageBox.Show("Тест «" + name + "» уже существует. Перезаписать?", "SkillChecker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            string json = JsonSerializer.Serialize(questions, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool IsValidName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalid) >= 0)
            {
                return false;
            }
            if (name == "test_settings")
            {
                return false;
            }
            return true;
        }

        private string ValidateQuestion(EditorQuestion question, int number)
        {
            if (question.Text.Trim().Length == 0)
            {
                return "Вопрос " + number + ": не указан текст";
            }

            if (question.Type == QuestionTypes.Text)
            {
                if (CountAcceptable(question.AcceptableText) == 0)
                {
                    return "Вопрос " + number + ": добавьте хотя бы один допустимый ответ";
                }
                return "";
            }

            if (question.Options.Count < 2)
            {
                return "Вопрос " + number + ": нужно минимум 2 варианта ответа";
            }
            for (int i = 0; i < question.Options.Count; i++)
            {
                if (question.Options[i].Text.Trim().Length == 0)
                {
                    return "Вопрос " + number + ": заполните все варианты ответа";
                }
            }

            int correctCount = 0;
            for (int i = 0; i < question.Options.Count; i++)
            {
                if (question.Options[i].IsCorrect)
                {
                    correctCount++;
                }
            }
            if (correctCount == 0)
            {
                return "Вопрос " + number + ": отметьте правильный ответ";
            }
            if (question.Type == QuestionTypes.Single && correctCount > 1)
            {
                return "Вопрос " + number + ": для одиночного выбора только один правильный ответ";
            }
            return "";
        }

        private int CountAcceptable(string text)
        {
            int count = 0;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length > 0)
                {
                    count++;
                }
            }
            return count;
        }

        private EditorQuestion ToEditorQuestion(Question question)
        {
            EditorQuestion editorQuestion = new EditorQuestion();
            editorQuestion.Text = question.Text;
            editorQuestion.Type = question.Type;

            if (question.Type == QuestionTypes.Text)
            {
                editorQuestion.AcceptableText = string.Join("\n", question.AcceptableAnswers);
            }
            else
            {
                for (int i = 0; i < question.Options.Count; i++)
                {
                    EditorOption option = new EditorOption();
                    option.Text = question.Options[i];
                    if (question.Type == QuestionTypes.Multiple)
                    {
                        option.IsCorrect = question.CorrectAnswerIndices.Contains(i);
                    }
                    else
                    {
                        option.IsCorrect = i == question.CorrectAnswerIndex;
                    }
                    editorQuestion.Options.Add(option);
                }
            }
            return editorQuestion;
        }

        private Question ToQuestion(EditorQuestion editorQuestion)
        {
            Question question = new Question();
            question.Text = editorQuestion.Text.Trim();
            question.Type = editorQuestion.Type;

            if (editorQuestion.Type == QuestionTypes.Text)
            {
                question.AcceptableAnswers = new List<string>();
                string[] lines = editorQuestion.AcceptableText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length > 0)
                    {
                        question.AcceptableAnswers.Add(line);
                    }
                }
            }
            else
            {
                question.Options = new List<string>();
                question.CorrectAnswerIndices = new List<int>();
                for (int i = 0; i < editorQuestion.Options.Count; i++)
                {
                    question.Options.Add(editorQuestion.Options[i].Text.Trim());
                    if (editorQuestion.Options[i].IsCorrect)
                    {
                        question.CorrectAnswerIndices.Add(i);
                    }
                }
                if (editorQuestion.Type == QuestionTypes.Single && question.CorrectAnswerIndices.Count > 0)
                {
                    question.CorrectAnswerIndex = question.CorrectAnswerIndices[0];
                }
            }
            return question;
        }
    }
}
