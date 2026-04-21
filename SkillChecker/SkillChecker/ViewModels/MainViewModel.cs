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

        private string _resultScore;
        private string _resultCorrect;

        private Visibility _authVisibility;
        private Visibility _testingVisibility;
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

            _resultScore = "";
            _resultCorrect = "";

            _authVisibility = Visibility.Visible;
            _testingVisibility = Visibility.Collapsed;
            _resultVisibility = Visibility.Collapsed;

            ConnectCommand = new RelayCommand(ExecuteConnect);
            StartTestCommand = new RelayCommand(ExecuteStartTest, CanStartTest);
            NextQuestionCommand = new RelayCommand(ExecuteNextQuestion, CanNextQuestion);
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

        public Visibility ResultVisibility
        {
            get => _resultVisibility;
            set { _resultVisibility = value; OnPropertyChanged(); }
        }

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand StartTestCommand { get; private set; }
        public RelayCommand NextQuestionCommand { get; private set; }
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
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Testing")
            {
                AuthVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Visible;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Result")
            {
                AuthVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
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
            _selectedAnswers.Add(SelectedOptionIndex);
            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questions.Count)
            {
                ShowQuestion(_currentQuestionIndex);
            }
            else
            {
                ShowResult();
            }
        }

        private void ShowQuestion(int index)
        {
            Question q = _questions[index];
            QuestionText = q.Text;
            CurrentOptions = q.Options;
            SelectedOptionIndex = -1;
            QuestionNumber = index + 1;
        }

        private void ShowResult()
        {
            try
            {
                TestResult result = _clientService.SubmitAnswers(StudentName, StudentGroup, _selectedTestName, _selectedAnswers);

                ResultScore = "Оценка: " + result.Score + "%";
                ResultCorrect = "Правильных ответов: " + result.CorrectAnswers + " из " + result.TotalQuestions;
                AppState = "Result";
            }
            catch (Exception ex)
            {
                ResultScore = "Ошибка";
                ResultCorrect = ex.Message;
                AppState = "Result";
            }
        }

        private void ExecuteRestart(object? parameter)
        {
            _selectedAnswers = new List<int>();
            _currentQuestionIndex = 0;
            SelectedOptionIndex = -1;
            SelectedTestName = "";
            StatusMessage = "Введите IP сервера и подключитесь";
            AppState = "Auth";
        }

        private void ExecuteExit(object? parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
