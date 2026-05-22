using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using SkillChecker.Commands;
using SkillChecker.Common.Models;
using SkillChecker.Models;
using SkillChecker.Services;

namespace SkillChecker.ViewModels
{
    public enum AppState
    {
        Auth,
        Wait,
        Testing,
        Review,
        Result
    }

    public partial class MainViewModel : INotifyPropertyChanged
    {
        private ClientService _clientService;
        private List<Question> _questions;
        private List<List<int>> _selectedAnswers;
        private List<string> _textAnswers;
        private int _currentQuestionIndex;
        private string _selectedTestName;
        private DispatcherTimer _waitTimer;
        private DispatcherTimer _testTimer;
        private DateTime _testEndTime;
        private int _testTimeMinutes;

        private AppState _appState;
        private string _serverIp;
        private string _serverPort;
        private string _studentName;
        private string _studentGroup;
        private string _statusMessage;
        private bool _isConnected;
        private string _testCountText;
        private bool _isNameError;
        private bool _isGroupError;
        private List<string> _testNames;
        private List<ScheduledTest> _scheduledTests;
        private List<TestCardItem> _testCards;

        private string _questionText;
        private string _questionTypeHint;
        private List<OptionItem> _currentOptions;
        private int _selectedOptionIndex;
        private bool _isCurrentMultiple;
        private bool _isCurrentText;
        private string _currentTextAnswer;
        private List<int> _currentMultipleSelected;
        private int _questionNumber;
        private int _totalQuestions;
        private double _progressValue;

        private string _nextButtonText;
        private bool _canGoBack;

        private string _waitTestName;
        private string _waitTimeText;
        private string _waitCountdownText;
        private bool _canStartFromWait;
        private string _waitButtonHelpText;

        private string _timerText;
        private Visibility _timerVisibility;

        private string _resultScore;
        private string _resultCorrect;
        private int _scoreLevel;
        private bool _isPerfectScore;
        private List<ResultItem> _resultItems;

        private List<ReviewItem> _reviewItems;

        private Visibility _authVisibility;
        private Visibility _waitVisibility;
        private Visibility _testingVisibility;
        private Visibility _reviewVisibility;
        private Visibility _resultVisibility;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            _clientService = new ClientService();
            _questions = new List<Question>();
            _selectedAnswers = new List<List<int>>();
            _textAnswers = new List<string>();
            _currentQuestionIndex = 0;
            _selectedTestName = "";
            _waitTimer = new DispatcherTimer();
            _waitTimer.Interval = TimeSpan.FromSeconds(1);
            _waitTimer.Tick += WaitTimerTick;

            _testTimer = new DispatcherTimer();
            _testTimer.Interval = TimeSpan.FromSeconds(1);
            _testTimer.Tick += TestTimerTick;

            _testTimeMinutes = 0;

            _appState = AppState.Auth;
            _serverIp = "127.0.0.1";
            _serverPort = "9000";
            _studentName = "";
            _studentGroup = "";
            _statusMessage = "Введите IP сервера и подключитесь";
            _isConnected = false;
            _testCountText = "";
            _isNameError = false;
            _isGroupError = false;
            _testNames = new List<string>();
            _scheduledTests = new List<ScheduledTest>();
            _testCards = new List<TestCardItem>();

            _questionText = "";
            _questionTypeHint = "";
            _currentOptions = new List<OptionItem>();
            _selectedOptionIndex = -1;
            _isCurrentMultiple = false;
            _isCurrentText = false;
            _currentTextAnswer = "";
            _currentMultipleSelected = new List<int>();
            _questionNumber = 0;
            _totalQuestions = 0;
            _progressValue = 0;
            _nextButtonText = "Далее";
            _canGoBack = false;

            _waitTestName = "";
            _waitTimeText = "";
            _waitCountdownText = "";
            _canStartFromWait = false;
            _waitButtonHelpText = "Кнопка станет активной когда наступит время начала теста";

            _timerText = "";
            _timerVisibility = Visibility.Collapsed;

            _resultScore = "";
            _resultCorrect = "";
            _scoreLevel = 0;
            _isPerfectScore = false;
            _resultItems = new List<ResultItem>();
            _reviewItems = new List<ReviewItem>();

            _authVisibility = Visibility.Visible;
            _waitVisibility = Visibility.Collapsed;
            _testingVisibility = Visibility.Collapsed;
            _reviewVisibility = Visibility.Collapsed;
            _resultVisibility = Visibility.Collapsed;

            ConnectCommand = new RelayCommand(ExecuteConnect);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanRefresh);
            StartTestCommand = new RelayCommand(ExecuteStartTest, CanStartTest);
            NextQuestionCommand = new RelayCommand(ExecuteNextQuestion, CanNextQuestion);
            PrevQuestionCommand = new RelayCommand(ExecutePrevQuestion, CanPrevQuestion);
            GoToQuestionCommand = new RelayCommand(ExecuteGoToQuestion);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            SelectTestCardCommand = new RelayCommand(ExecuteSelectTestCard);
            StartFromWaitCommand = new RelayCommand(ExecuteStartFromWait, CheckCanStartFromWait);
            ToggleOptionCommand = new RelayCommand(ExecuteToggleOption);
            CancelTestCommand = new RelayCommand(ExecuteCancelTest);
            RestartCommand = new RelayCommand(ExecuteRestart);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        public AppState AppState
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
            set { _studentName = value; IsNameError = false; OnPropertyChanged(); }
        }

        public string StudentGroup
        {
            get => _studentGroup;
            set { _studentGroup = value; IsGroupError = false; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public string TestCountText
        {
            get => _testCountText;
            set { _testCountText = value; OnPropertyChanged(); }
        }

        public bool IsNameError
        {
            get => _isNameError;
            set { _isNameError = value; OnPropertyChanged(); }
        }

        public bool IsGroupError
        {
            get => _isGroupError;
            set { _isGroupError = value; OnPropertyChanged(); }
        }

        public List<string> TestNames
        {
            get => _testNames;
            set { _testNames = value; OnPropertyChanged(); }
        }

        public List<ScheduledTest> ScheduledTests
        {
            get => _scheduledTests;
            set { _scheduledTests = value; OnPropertyChanged(); }
        }

        public List<TestCardItem> TestCards
        {
            get => _testCards;
            set { _testCards = value; OnPropertyChanged(); }
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

        public string QuestionTypeHint
        {
            get => _questionTypeHint;
            set { _questionTypeHint = value; OnPropertyChanged(); }
        }

        public List<OptionItem> CurrentOptions
        {
            get => _currentOptions;
            set { _currentOptions = value; OnPropertyChanged(); }
        }

        public int SelectedOptionIndex
        {
            get => _selectedOptionIndex;
            set { _selectedOptionIndex = value; OnPropertyChanged(); }
        }

        public bool IsCurrentMultiple
        {
            get => _isCurrentMultiple;
            set { _isCurrentMultiple = value; OnPropertyChanged(); }
        }

        public bool IsCurrentText
        {
            get => _isCurrentText;
            set { _isCurrentText = value; OnPropertyChanged(); }
        }

        public string CurrentTextAnswer
        {
            get => _currentTextAnswer;
            set { _currentTextAnswer = value; OnPropertyChanged(); }
        }

        public List<int> CurrentMultipleSelected
        {
            get => _currentMultipleSelected;
            set { _currentMultipleSelected = value; OnPropertyChanged(); }
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

        public string WaitTestName
        {
            get => _waitTestName;
            set { _waitTestName = value; OnPropertyChanged(); }
        }

        public string WaitTimeText
        {
            get => _waitTimeText;
            set { _waitTimeText = value; OnPropertyChanged(); }
        }

        public string WaitCountdownText
        {
            get => _waitCountdownText;
            set { _waitCountdownText = value; OnPropertyChanged(); }
        }

        public bool CanStartFromWait
        {
            get => _canStartFromWait;
            set { _canStartFromWait = value; OnPropertyChanged(); UpdateWaitButtonHelpText(); }
        }

        public string WaitButtonHelpText
        {
            get => _waitButtonHelpText;
            set { _waitButtonHelpText = value; OnPropertyChanged(); }
        }

        public string TimerText
        {
            get => _timerText;
            set { _timerText = value; OnPropertyChanged(); }
        }

        public Visibility TimerVisibility
        {
            get => _timerVisibility;
            set { _timerVisibility = value; OnPropertyChanged(); }
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

        public int ScoreLevel
        {
            get => _scoreLevel;
            set { _scoreLevel = value; OnPropertyChanged(); }
        }

        public bool IsPerfectScore
        {
            get => _isPerfectScore;
            set { _isPerfectScore = value; OnPropertyChanged(); }
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

        public Visibility WaitVisibility
        {
            get => _waitVisibility;
            set { _waitVisibility = value; OnPropertyChanged(); }
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
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand StartTestCommand { get; private set; }
        public RelayCommand NextQuestionCommand { get; private set; }
        public RelayCommand PrevQuestionCommand { get; private set; }
        public RelayCommand GoToQuestionCommand { get; private set; }
        public RelayCommand SubmitCommand { get; private set; }
        public RelayCommand SelectTestCardCommand { get; private set; }
        public RelayCommand StartFromWaitCommand { get; private set; }
        public RelayCommand ToggleOptionCommand { get; private set; }
        public RelayCommand CancelTestCommand { get; private set; }
        public RelayCommand RestartCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateVisibility()
        {
            if (_appState == AppState.Auth)
            {
                AuthVisibility = Visibility.Visible;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == AppState.Wait)
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Visible;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == AppState.Testing)
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Visible;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == AppState.Review)
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Visible;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == AppState.Result)
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Visible;
            }
        }
    }
}
