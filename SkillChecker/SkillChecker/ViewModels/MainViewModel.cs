using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
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
        private DispatcherTimer _waitTimer;
        private DispatcherTimer _testTimer;
        private DateTime _testEndTime;
        private int _testTimeMinutes;

        private string _appState;
        private string _serverIp;
        private string _serverPort;
        private string _studentName;
        private string _studentGroup;
        private string _statusMessage;
        private List<string> _testNames;
        private List<ScheduledTest> _scheduledTests;
        private List<TestCardItem> _testCards;

        private string _questionText;
        private List<string> _currentOptions;
        private int _selectedOptionIndex;
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
            _selectedAnswers = new List<int>();
            _selectedTestName = "";
            _waitTimer = new DispatcherTimer();
            _waitTimer.Interval = TimeSpan.FromSeconds(1);
            _waitTimer.Tick += WaitTimerTick;

            _testTimer = new DispatcherTimer();
            _testTimer.Interval = TimeSpan.FromSeconds(1);
            _testTimer.Tick += TestTimerTick;

            _testTimeMinutes = 0;

            _appState = "Auth";
            _serverIp = "127.0.0.1";
            _serverPort = "9000";
            _studentName = "";
            _studentGroup = "";
            _statusMessage = "Введите IP сервера и подключитесь";
            _testNames = new List<string>();
            _scheduledTests = new List<ScheduledTest>();
            _testCards = new List<TestCardItem>();

            _questionText = "";
            _currentOptions = new List<string>();
            _selectedOptionIndex = -1;
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
            _resultItems = new List<ResultItem>();
            _reviewItems = new List<ReviewItem>();

            _authVisibility = Visibility.Visible;
            _waitVisibility = Visibility.Collapsed;
            _testingVisibility = Visibility.Collapsed;
            _reviewVisibility = Visibility.Collapsed;
            _resultVisibility = Visibility.Collapsed;

            ConnectCommand = new RelayCommand(ExecuteConnect);
            StartTestCommand = new RelayCommand(ExecuteStartTest, CanStartTest);
            NextQuestionCommand = new RelayCommand(ExecuteNextQuestion, CanNextQuestion);
            PrevQuestionCommand = new RelayCommand(ExecutePrevQuestion, CanPrevQuestion);
            GoToQuestionCommand = new RelayCommand(ExecuteGoToQuestion);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            SelectTestCardCommand = new RelayCommand(ExecuteSelectTestCard);
            StartFromWaitCommand = new RelayCommand(ExecuteStartFromWait, CheckCanStartFromWait);
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
        public RelayCommand StartTestCommand { get; private set; }
        public RelayCommand NextQuestionCommand { get; private set; }
        public RelayCommand PrevQuestionCommand { get; private set; }
        public RelayCommand GoToQuestionCommand { get; private set; }
        public RelayCommand SubmitCommand { get; private set; }
        public RelayCommand SelectTestCardCommand { get; private set; }
        public RelayCommand StartFromWaitCommand { get; private set; }
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
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Wait")
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Visible;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Testing")
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Visible;
                ReviewVisibility = Visibility.Collapsed;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Review")
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
                TestingVisibility = Visibility.Collapsed;
                ReviewVisibility = Visibility.Visible;
                ResultVisibility = Visibility.Collapsed;
            }
            else if (_appState == "Result")
            {
                AuthVisibility = Visibility.Collapsed;
                WaitVisibility = Visibility.Collapsed;
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

                List<ScheduledTest> scheduled = _clientService.GetScheduledTests();
                ScheduledTests = scheduled;

                BuildTestCards(tests, scheduled);

                if (tests.Count > 0)
                {
                    StatusMessage = "Подключено. Доступно тестов: " + tests.Count;
                }
                else
                {
                    StatusMessage = "Подключено, но тестов на сервере нет";
                }

                if (scheduled.Count > 0)
                {
                    StatusMessage += ". Запланировано: " + scheduled.Count;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка подключения: " + ex.Message;
            }
        }

        private void BuildTestCards(List<string> tests, List<ScheduledTest> scheduled)
        {
            List<TestCardItem> cards = new List<TestCardItem>();

            for (int i = 0; i < tests.Count; i++)
            {
                TestCardItem card = new TestCardItem();
                card.Name = tests[i];
                card.IsScheduled = false;
                card.ScheduleTime = "";
                card.TimeMinutes = 0;

                for (int j = 0; j < scheduled.Count; j++)
                {
                    if (scheduled[j].Name == tests[i])
                    {
                        card.TimeMinutes = scheduled[j].TimeMinutes;
                        if (scheduled[j].ScheduledTime > DateTime.Now)
                        {
                            card.IsScheduled = true;
                            card.ScheduleTime = scheduled[j].ScheduledTime.ToString("HH:mm");
                        }
                        break;
                    }
                }

                cards.Add(card);
            }

            TestCards = cards;
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
            StartTestByName(_selectedTestName);
        }

        private void ExecuteSelectTestCard(object? parameter)
        {
            if (parameter != null)
            {
                string testName = parameter.ToString() ?? "";
                if (testName.Length > 0)
                {
                    if (string.IsNullOrWhiteSpace(StudentName) || string.IsNullOrWhiteSpace(StudentGroup))
                    {
                        StatusMessage = "Введите ФИО и группу перед началом теста";
                        return;
                    }

                    _selectedTestName = testName;

                    bool isScheduled = false;
                    for (int i = 0; i < _testCards.Count; i++)
                    {
                        if (_testCards[i].Name == testName && _testCards[i].IsScheduled)
                        {
                            isScheduled = true;
                            break;
                        }
                    }

                    if (isScheduled)
                    {
                        ScheduledTest? sched = null;
                        for (int i = 0; i < _scheduledTests.Count; i++)
                        {
                            if (_scheduledTests[i].Name == testName)
                            {
                                sched = _scheduledTests[i];
                                break;
                            }
                        }

                        if (sched != null && sched.ScheduledTime > DateTime.Now)
                        {
                            ShowWaitScreen(testName, sched.ScheduledTime, sched.TimeMinutes);
                            return;
                        }
                    }

                    StartTestByName(testName);
                }
            }
        }

        private void StartTestByName(string testName)
        {
            try
            {
                TestQuestionsResult testResult = _clientService.GetTestQuestions(testName);

                if (testResult.IsWaiting)
                {
                    ShowWaitScreen(testName, testResult.WaitTime, testResult.TimeMinutes);
                    return;
                }

                if (testResult.Questions.Count == 0)
                {
                    StatusMessage = "Тест пуст";
                    return;
                }

                _questions = testResult.Questions;
                _selectedAnswers = new List<int>();
                for (int i = 0; i < _questions.Count; i++)
                {
                    _selectedAnswers.Add(-1);
                }
                _currentQuestionIndex = 0;
                TotalQuestions = _questions.Count;
                ShowQuestion(0);
                AppState = "Testing";

                _testTimeMinutes = testResult.TimeMinutes;
                if (_testTimeMinutes > 0)
                {
                    _testEndTime = DateTime.Now.AddMinutes(_testTimeMinutes);
                    TimerVisibility = Visibility.Visible;
                    UpdateTestTimer();
                    _testTimer.Start();
                }
                else
                {
                    TimerVisibility = Visibility.Collapsed;
                    TimerText = "";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void TestTimerTick(object? sender, EventArgs e)
        {
            UpdateTestTimer();
        }

        private void UpdateTestTimer()
        {
            TimeSpan remaining = _testEndTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                TimerText = "00:00";
                _testTimer.Stop();
                AutoSubmit();
                return;
            }

            int totalMinutes = (int)remaining.TotalMinutes;
            int seconds = remaining.Seconds;
            TimerText = totalMinutes.ToString("D2") + ":" + seconds.ToString("D2");
        }

        private void AutoSubmit()
        {
            if (SelectedOptionIndex >= 0)
            {
                _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
            }

            MessageBox.Show("Время вышло! Ответы отправлены.", "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowResult();
        }

        private void ShowWaitScreen(string testName, DateTime scheduledTime, int timeMinutes)
        {
            WaitTestName = testName;
            WaitTimeText = "Тест начнётся в " + scheduledTime.ToString("HH:mm");
            if (timeMinutes > 0)
            {
                WaitTimeText += " (лимит: " + timeMinutes + " мин)";
            }
            UpdateWaitCountdown(scheduledTime);
            _waitTimer.Start();
            AppState = "Wait";
        }

        private void WaitTimerTick(object? sender, EventArgs e)
        {
            ScheduledTest? scheduled = null;
            for (int i = 0; i < _scheduledTests.Count; i++)
            {
                if (_scheduledTests[i].Name == _waitTestName)
                {
                    scheduled = _scheduledTests[i];
                    break;
                }
            }

            if (scheduled != null)
            {
                UpdateWaitCountdown(scheduled.ScheduledTime);
            }
        }

        private void UpdateWaitCountdown(DateTime scheduledTime)
        {
            TimeSpan remaining = scheduledTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                WaitCountdownText = "00:00:00";
                CanStartFromWait = true;
                _waitTimer.Stop();
            }
            else
            {
                int hours = (int)remaining.TotalHours;
                int minutes = remaining.Minutes;
                int seconds = remaining.Seconds;
                WaitCountdownText = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
                CanStartFromWait = false;
            }
        }

        private void UpdateWaitButtonHelpText()
        {
            if (_canStartFromWait)
            {
                WaitButtonHelpText = "Время наступило, нажмите чтобы начать тест";
            }
            else
            {
                WaitButtonHelpText = "Кнопка станет активной когда наступит время начала теста";
            }
        }

        private bool CheckCanStartFromWait(object? parameter)
        {
            return _canStartFromWait;
        }

        private void ExecuteStartFromWait(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(StudentName) || string.IsNullOrWhiteSpace(StudentGroup))
            {
                StatusMessage = "Введите ФИО и группу перед началом теста";
                _waitTimer.Stop();
                AppState = "Auth";
                return;
            }

            _waitTimer.Stop();
            StartTestByName(_waitTestName);
        }

        private bool CanNextQuestion(object? parameter)
        {
            return true;
        }

        private void ExecuteNextQuestion(object? parameter)
        {
            if (SelectedOptionIndex >= 0)
            {
                _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
            }

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
            if (SelectedOptionIndex >= 0)
            {
                _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
            }
            ShowQuestion(_currentQuestionIndex - 1);
        }

        private void ExecuteGoToQuestion(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int index))
            {
                if (index >= 0 && index < _questions.Count)
                {
                    if (SelectedOptionIndex >= 0)
                    {
                        _selectedAnswers[_currentQuestionIndex] = SelectedOptionIndex;
                    }
                    ShowQuestion(index);
                    AppState = "Testing";
                }
            }
        }

        private void ExecuteSubmit(object? parameter)
        {
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
            NextButtonText = "Далее";

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
                item.ReviewAccessibilityName = "Вопрос " + (i + 1) + ": " + _questions[i].Text + (_selectedAnswers[i] >= 0 ? ", отвечен" : ", не отвечен");
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
            _testTimer.Stop();
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
            _waitTimer.Stop();
            _testTimer.Stop();
            _selectedAnswers = new List<int>();
            _currentQuestionIndex = 0;
            SelectedOptionIndex = -1;
            SelectedTestName = "";
            ProgressValue = 0;
            TimerVisibility = Visibility.Collapsed;
            TimerText = "";
            StatusMessage = "Введите IP сервера и подключитесь";
            AppState = "Auth";
        }

        private void ExecuteExit(object? parameter)
        {
            Application.Current.Shutdown();
        }
    }

    public class TestCardItem
    {
        private string _name;
        private bool _isScheduled;
        private string _scheduleTime;
        private int _timeMinutes;

        public string Name { get => _name; set => _name = value; }
        public bool IsScheduled { get => _isScheduled; set => _isScheduled = value; }
        public string ScheduleTime { get => _scheduleTime; set => _scheduleTime = value; }
        public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }

        public TestCardItem()
        {
            _name = "";
            _isScheduled = false;
            _scheduleTime = "";
            _timeMinutes = 0;
        }
    }

    public class ReviewItem
    {
        private string _number;
        private string _questionText;
        private bool _isAnswered;
        private int _index;
        private string _accessibilityName;

        public string Number { get => _number; set => _number = value; }
        public string QuestionText { get => _questionText; set => _questionText = value; }
        public bool IsAnswered { get => _isAnswered; set => _isAnswered = value; }
        public int Index { get => _index; set => _index = value; }
        public string ReviewAccessibilityName { get => _accessibilityName; set => _accessibilityName = value; }

        public ReviewItem()
        {
            _number = "";
            _questionText = "";
            _isAnswered = false;
            _index = 0;
            _accessibilityName = "";
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
