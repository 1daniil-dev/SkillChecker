using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SkillChecker.Commands;
using SkillChecker.Common.Models;
using SkillChecker.Services;

namespace SkillChecker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ClientService _clientService;
        private List<Question> _questions;
        private List<int> _selectedAnswers;
        private int _currentQuestionIndex;
        private string _selectedTestName;

        private string _appState;
        private string _serverIp;
        private string _serverPort;
        private string _studentName;
        private string _studentGroup;
        private string _statusMessage;
        private List<string> _testNames;

        private string _questionText;
        private List<string> _currentOptions;
        private int _selectedOptionIndex;
        private int _questionNumber;
        private int _totalQuestions;
        private double _progressValue;

        private string _nextButtonText;
        private bool _canGoBack;

        private string _resultScore;
        private string _resultCorrect;
        private List<ResultItem> _resultItems;

        private List<ReviewItem> _reviewItems;

        private Visibility _authVisibility;
        private Visibility _testingVisibility;
        private Visibility _reviewVisibility;
        private Visibility _resultVisibility;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            _clientService = new ClientService();
            _questions = new List<Question>();
            _selectedAnswers = new List<int>();
            _selectedTestName = "";

            _appState = "Auth";
            _serverIp = "127.0.0.1";
            _serverPort = "9000";
            _studentName = "";
            _studentGroup = "";
            _statusMessage = "Введите IP сервера и подключитесь";
            _testNames = new List<string>();

            _questionText = "";
            _currentOptions = new List<string>();
            _selectedOptionIndex = -1;
            _questionNumber = 0;
            _totalQuestions = 0;
            _progressValue = 0;
            _nextButtonText = "Далее";
            _canGoBack = false;

            _resultScore = "";
            _resultCorrect = "";
            _resultItems = new List<ResultItem>();
            _reviewItems = new List<ReviewItem>();

            _authVisibility = Visibility.Visible;
            _testingVisibility = Visibility.Collapsed;
            _reviewVisibility = Visibility.Collapsed;
            _resultVisibility = Visibility.Collapsed;

            ConnectCommand = new RelayCommand(ExecuteConnect);
            StartTestCommand = new RelayCommand(ExecuteStartTest, CanStartTest);
            NextQuestionCommand = new RelayCommand(ExecuteNextQuestion, CanNextQuestion);
            PrevQuestionCommand = new RelayCommand(ExecutePrevQuestion, CanPrevQuestion);
            GoToQuestionCommand = new RelayCommand(ExecuteGoToQuestion);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            RestartCommand = new RelayCommand(ExecuteRestart);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        public string AppState
        {
            get => _appState;
            set { _appState = value; OnPropertyChanged(); UpdateVisibility(); }
        }

        public string ServerIp
        {
            get => _serverIp;
            set { _serverIp = value; OnPropertyChanged(); }
        }

        public string ServerPort
        {
            get => _serverPort;
            set { _serverPort = value; OnPropertyChanged(); }
        }

        public string StudentName
        {
            get => _studentName;
            set { _studentName = value; OnPropertyChanged(); }
        }

        public string StudentGroup
        {
            get => _studentGroup;
            set { _studentGroup = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public List<string> TestNames
        {
            get => _testNames;
            set { _testNames = value; OnPropertyChanged(); }
        }

        public string SelectedTestName
        {
            get => _selectedTestName;
            set { _selectedTestName = value; OnPropertyChanged(); }
        }

        public string QuestionText
        {
            get => _questionText;
            set { _questionText = value; OnPropertyChanged(); }
        }

        public List<string> CurrentOptions
        {
            get => _currentOptions;
            set { _currentOptions = value; OnPropertyChanged(); }
        }

        public int SelectedOptionIndex
        {
            get => _selectedOptionIndex;
            set { _selectedOptionIndex = value; OnPropertyChanged(); }
        }

        public int QuestionNumber
        {
            get => _questionNumber;
            set { _questionNumber = value; OnPropertyChanged(); }
        }

        public int TotalQuestions
        {
            get => _totalQuestions;
            set { _totalQuestions = value; OnPropertyChanged(); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string NextButtonText
        {
            get => _nextButtonText;
            set { _nextButtonText = value; OnPropertyChanged(); }
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            set { _canGoBack = value; OnPropertyChanged(); }
        }

        public string ResultScore
        {
            get => _resultScore;
            set { _resultScore = value; OnPropertyChanged(); }
        }

        public string ResultCorrect
        {
            get => _resultCorrect;
            set { _resultCorrect = value; OnPropertyChanged(); }
        }

        public List<ResultItem> ResultItems
        {
            get => _resultItems;
            set { _resultItems = value; OnPropertyChanged(); }
        }

        public List<ReviewItem> ReviewItems
        {
            get => _reviewItems;
            set { _reviewItems = value; OnPropertyChanged(); }
        }

        public Visibility AuthVisibility
        {
            get => _authVisibility;
            set { _authVisibility = value; OnPropertyChanged(); }
        }

        public Visibility TestingVisibility
        {
            get => _testingVisibility;
            set { _testingVisibility = value; OnPropertyChanged(); }
        }

        public Visibility ReviewVisibility
        {
            get => _reviewVisibility;
            set { _reviewVisibility = value; OnPropertyChanged(); }
        }

        public Visibility ResultVisibility
        {
            get => _resultVisibility;
            set { _resultVisibility = value; OnPropertyChanged(); }
        }

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand StartTestCommand { get; private set; }
        public RelayCommand NextQuestionCommand { get; private set; }
        public RelayCommand PrevQuestionCommand { get; private set; }
        public RelayCommand GoToQuestionCommand { get; private set; }
        public RelayCommand SubmitCommand { get; private set; }
        public RelayCommand RestartCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateVisibility()
        {
            if (_appState == "Auth")
            {
                AuthVisibility = Visibility.Visible;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Testing")
            {
                AuthVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Visible;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Review")
            {
                AuthVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Visible;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Result")
            {
                AuthVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Visible;
            }
        }

        private void ExecuteConnect(object? parameter)
        {
            try
            {
                _clientService.ServerIp = _serverIp;
                if (int.TryParse(_serverPort, out int port))
                {
                    _clientService.ServerPort = port;
                }

                List<string> tests = _clientService.GetTestList();
                TestNames = tests;

                if (tests.Count > 0)
                {
                    StatusMessage = "Подключено. Доступно тестов: " + tests.Count;
                }
                else
                {
                    StatusMessage = "Подключено, но тестов на сервере нет";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка подключения: " + ex.Message;
            }
        }

        private bool CanStartTest(object? parameter)
        {
            return TestNames.Count > 0
                && !string.IsNullOrWhiteSpace(StudentName)
                && !string.IsNullOrWhiteSpace(StudentGroup)
                && !string.IsNullOrWhiteSpace(SelectedTestName);
        }

        private void ExecuteStartTest(object? parameter)
        {
            try
            {
                List<Question> questions = _clientService.GetTestQuestions(_selectedTestName);

                if (questions.Count == 0)
                {
                    StatusMessage = "Тест пуст";
                    return;
                }

                _questions = questions;
                _selectedAnswers = new List<int>();
                for (int i = 0; i < questions.Count; i++)
                {
                    _selectedAnswers.Add(-1);
                }
                _currentQuestionIndex = 0;
                TotalQuestions = _questions.Count;
                ShowQuestion(0);
                AppState = "Testing";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private bool CanNextQuestion(object? parameter)
        {
            return SelectedOptionIndex >= 0;
        }

        private void ExecuteNextQuestion(object? parameter)
        {
            _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;

            if (_currentQuestionIndex < _questions.Count - 1)
            {
                ShowQuestion(_currentQuestionIndex + 1);
            }
            else
            {
                ShowReview();
            }
        }

        private bool CanPrevQuestion(object? parameter)
        {
            return CanGoBack;
        }

        private void ExecutePrevQuestion(object? parameter)
        {
            _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
            ShowQuestion(_currentQuestionIndex - 1);
        }

        private void ExecuteGoToQuestion(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int index))
            {
                if (index >= 0 && index < _questions.Count)
                {
                    _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
                    ShowQuestion(index);
                    AppState = "Testing";
                }
            }
        }

        private void ExecuteSubmit(object? parameter)
        {
            _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
            ShowResult();
        }

        private void ShowQuestion(int index)
        {
            _currentQuestionIndex = index;
            Question q = _questions[index];
            QuestionText = q.Text;
            CurrentOptions = q.Options;
            SelectedOptionIndex = _selectedAnswers[index];
            QuestionNumber = index + 1;
            CanGoBack = index > 0;

            if (index < _questions.Count - 1)
            {
                NextButtonText = "Далее";
            }
            else
            {
                NextButtonText = "Завершить тест";
            }

            if (_questions.Count > 0)
            {
                ProgressValue = (double)(index + 1) / _questions.Count * 100;
            }
        }

        private void ShowReview()
        {
            List<ReviewItem> items = new List<ReviewItem>();
            int answeredCount = 0;

            for (int i = 0; i < _questions.Count; i++)
            {
                ReviewItem item = new ReviewItem();
                item.Number = (i + 1).ToString();
                item.QuestionText = _questions[i].Text;
                item.IsAnswered = _selectedAnswers[i] >= 0;
                item.Index = i;
                items.Add(item);

                if (_selectedAnswers[i] >= 0)
                {
                    answeredCount++;
                }
            }

            ReviewItems = items;
            AppState = "Review";
        }

        private void ShowResult()
        {
            ProgressValue = 100;

            try
            {
                TestResult result = _clientService.SubmitAnswers(StudentName, StudentGroup, _selectedTestName, _selectedAnswers);

                ResultScore = result.Score + "%";
                ResultCorrect = "Правильных ответов: " + result.CorrectAnswers + " из " + result.TotalQuestions;

                List<ResultItem> items = new List<ResultItem>();
                for (int i = 0; i < _questions.Count; i++)
                {
                    Question q = _questions[i];
                    int selected = i < _selectedAnswers.Count ? _selectedAnswers[i] : -1;
                    int correctIdx = i < result.Answers.Count ? result.Answers[i].CorrectIndex : 0;
                    bool isCorrect = selected == correctIdx;

                    string selectedText = selected >= 0 && selected < q.Options.Count ? q.Options[selected] : "Пропущен";
                    string correctText = correctIdx < q.Options.Count ? q.Options[correctIdx] : "?";

                    ResultItem item = new ResultItem();
                    item.Number = (i + 1).ToString();
                    item.QuestionText = q.Text;
                    item.SelectedAnswer = selectedText;
                    item.CorrectAnswer = correctText;
                    item.IsCorrect = isCorrect;
                    items.Add(item);
                }

                ResultItems = items;
                AppState = "Result";
            }
            catch (Exception ex)
            {
                ResultScore = "Ошибка";
                ResultCorrect = ex.Message;
                ResultItems = new List<ResultItem>();
                AppState = "Result";
            }
        }

        private void ExecuteRestart(object? parameter)
        {
            _selectedAnswers = new List<int>();
            _currentQuestionIndex = 0;
            SelectedOptionIndex = -1;
            SelectedTestName = "";
            ProgressValue = 0;
            StatusMessage = "Введите IP сервера и подключитесь";
            AppState = "Auth";
        }

        private void ExecuteExit(object? parameter)
        {
            Application.Current.Shutdown();
        }
    }

    public class ReviewItem
    {
        private string _number;
        private string _questionText;
        private bool _isAnswered;
        private int _index;

        public string Number { get => _number; set => _number = value; }
        public string QuestionText { get => _questionText; set => _questionText = value; }
        public bool IsAnswered { get => _isAnswered; set => _isAnswered = value; }
        public int Index { get => _index; set => _index = value; }

        public ReviewItem()
        {
            _number = "";
            _questionText = "";
            _isAnswered = false;
            _index = 0;
        }
    }

    public class ResultItem
    {
        private string _number;
        private string _questionText;
        private string _selectedAnswer;
        private string _correctAnswer;
        private bool _isCorrect;

        public string Number { get => _number; set => _number = value; }
        public string QuestionText { get => _questionText; set => _questionText = value; }
        public string SelectedAnswer { get => _selectedAnswer; set => _selectedAnswer = value; }
        public string CorrectAnswer { get => _correctAnswer; set => _correctAnswer = value; }
        public bool IsCorrect { get => _isCorrect; set => _isCorrect = value; }

        public ResultItem()
        {
            _number = "";
            _questionText = "";
            _selectedAnswer = "";
            _correctAnswer = "";
            _isCorrect = false;
        }
    }
}
