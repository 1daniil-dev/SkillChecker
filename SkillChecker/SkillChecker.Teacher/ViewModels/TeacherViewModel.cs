using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Win32;
using SkillChecker.Common.Models;
using SkillChecker.Data;
using SkillChecker.Teacher.Commands;
using SkillChecker.Teacher.Models;
using SkillChecker.Web;
using SkillChecker.Web.Services;
using SkillCheckerServer;

namespace SkillChecker.Teacher.ViewModels
{
    public class TeacherViewModel : INotifyPropertyChanged
    {
        private const int ServerPort = 9000;
        private const string PanelUrl = "http://localhost:5000";

        private Server _server = null!;
        private WebApplication _webApp = null!;
        private List<string> _ipList = new List<string>();
        private bool _isClosing;

        private string _serverStatusText = "Сервер остановлен";
        private bool _isServerRunning;
        private string _ipAddressText = "";
        private string _portText = "";
        private string _copyButtonText = "Копировать";
        private string _testsInfoText = "Тестов: 0";
        private string _totalValue = "0";
        private string _averageValue = "—";
        private string _bestValue = "—";
        private string _worstValue = "—";
        private string _searchText = "";
        private string _dateSortLabel = "По дате";
        private string _scoreSortLabel = "По баллу";
        private string _emptyText = "Пока никто не сдал";
        private string _sortField = "date";
        private bool _sortAscending;
        private ResultRow? _selectedResult;
        private TestInfo? _selectedTest;

        public ObservableCollection<TestInfo> Tests { get; } = new ObservableCollection<TestInfo>();
        public ObservableCollection<ResultRow> Results { get; } = new ObservableCollection<ResultRow>();
        public ObservableCollection<AnswerRow> SelectedAnswers { get; } = new ObservableCollection<AnswerRow>();
        public ICollectionView ResultsView { get; }

        public RelayCommand CopyAddressCommand { get; }
        public RelayCommand OpenPanelCommand { get; }
        public RelayCommand CreateTestCommand { get; }
        public RelayCommand EditTestCommand { get; }
        public RelayCommand SettingsTestCommand { get; }
        public RelayCommand PreviewTestCommand { get; }
        public RelayCommand DuplicateTestCommand { get; }
        public RelayCommand DeleteTestCommand { get; }
        public RelayCommand ImportTestCommand { get; }
        public RelayCommand RefreshTestsCommand { get; }
        public RelayCommand RefreshResultsCommand { get; }
        public RelayCommand ExportResultsCommand { get; }
        public RelayCommand ClearResultsCommand { get; }
        public RelayCommand DeleteResultCommand { get; }
        public RelayCommand SortByDateCommand { get; }
        public RelayCommand SortByScoreCommand { get; }

        public event Action<string?>? OpenEditorRequested;
        public event Action<string>? OpenSettingsRequested;
        public event Action<string>? OpenPreviewRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        public TeacherViewModel()
        {
            CopyAddressCommand = new RelayCommand(async p => await CopyAddressAsync());
            OpenPanelCommand = new RelayCommand(p => OpenPanel());
            CreateTestCommand = new RelayCommand(p => OpenEditorRequested?.Invoke(null));
            EditTestCommand = new RelayCommand(p => OpenEditorRequested?.Invoke(SelectedTest?.Name), p => SelectedTest != null);
            SettingsTestCommand = new RelayCommand(p =>
            {
                if (SelectedTest != null)
                {
                    OpenSettingsRequested?.Invoke(SelectedTest.Name);
                }
            }, p => SelectedTest != null);
            PreviewTestCommand = new RelayCommand(p =>
            {
                if (SelectedTest != null)
                {
                    OpenPreviewRequested?.Invoke(SelectedTest.Name);
                }
            }, p => SelectedTest != null);
            DuplicateTestCommand = new RelayCommand(p => DuplicateTest(), p => SelectedTest != null);
            DeleteTestCommand = new RelayCommand(p => DeleteTest(), p => SelectedTest != null);
            ImportTestCommand = new RelayCommand(p => ImportTest());
            RefreshTestsCommand = new RelayCommand(p => LoadTests());
            RefreshResultsCommand = new RelayCommand(p => LoadResults());
            ExportResultsCommand = new RelayCommand(p => ExportResults(), p => Results.Count > 0);
            ClearResultsCommand = new RelayCommand(p => ClearResults(), p => Results.Count > 0);
            DeleteResultCommand = new RelayCommand(p => DeleteResult(), p => SelectedResult != null);
            SortByDateCommand = new RelayCommand(p => SortBy("date"));
            SortByScoreCommand = new RelayCommand(p => SortBy("score"));

            ResultsView = CollectionViewSource.GetDefaultView(Results);
            ResultsView.Filter = FilterResult;
            ApplySort();

            StartServer();
            StartWebPanel();
            ShowAddresses();
            LoadTests();
            LoadResults();
            OpenPanelDelayed();
        }

        public string ServerStatusText
        {
            get => _serverStatusText;
            set { _serverStatusText = value; OnPropertyChanged(); }
        }

        public bool IsServerRunning
        {
            get => _isServerRunning;
            set { _isServerRunning = value; OnPropertyChanged(); }
        }

        public string IpAddressText
        {
            get => _ipAddressText;
            set { _ipAddressText = value; OnPropertyChanged(); }
        }

        public string PortText
        {
            get => _portText;
            set { _portText = value; OnPropertyChanged(); }
        }

        public string CopyButtonText
        {
            get => _copyButtonText;
            set { _copyButtonText = value; OnPropertyChanged(); }
        }

        public string TestsInfoText
        {
            get => _testsInfoText;
            set { _testsInfoText = value; OnPropertyChanged(); }
        }

        public string TotalValue
        {
            get => _totalValue;
            set { _totalValue = value; OnPropertyChanged(); }
        }

        public string AverageValue
        {
            get => _averageValue;
            set { _averageValue = value; OnPropertyChanged(); }
        }

        public string BestValue
        {
            get => _bestValue;
            set { _bestValue = value; OnPropertyChanged(); }
        }

        public string WorstValue
        {
            get => _worstValue;
            set { _worstValue = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged();
                ResultsView.Refresh();
                RefreshStats();
            }
        }

        public string DateSortLabel
        {
            get => _dateSortLabel;
            set { _dateSortLabel = value; OnPropertyChanged(); }
        }

        public string ScoreSortLabel
        {
            get => _scoreSortLabel;
            set { _scoreSortLabel = value; OnPropertyChanged(); }
        }

        public string EmptyText
        {
            get => _emptyText;
            set { _emptyText = value; OnPropertyChanged(); }
        }

        public ResultRow? SelectedResult
        {
            get => _selectedResult;
            set
            {
                _selectedResult = value;
                OnPropertyChanged();
                OnPropertyChanged("SelectedResultTitle");
                BuildAnswerRows(_selectedResult);
            }
        }

        public string SelectedResultTitle
        {
            get
            {
                if (_selectedResult == null)
                {
                    return "";
                }
                return "Ответы: " + _selectedResult.StudentName + " — " + _selectedResult.TestName;
            }
        }

        public TestInfo? SelectedTest
        {
            get => _selectedTest;
            set { _selectedTest = value; OnPropertyChanged(); }
        }

        public string TestsFolderPath { get => _server.TestsFolderPath; }

        public void Stop()
        {
            _isClosing = true;
            _server.Stop();
        }

        public void RefreshAfterEditor()
        {
            _server.LoadAllTests();
            LoadTests();
        }

        public TestSettings? GetTestSettingsFor(string testName)
        {
            Dictionary<string, TestSettings> settings = _server.GetTestSettings();
            TestSettings? result;
            if (settings.TryGetValue(testName, out result))
            {
                return result;
            }
            return null;
        }

        public void SaveSettingsForTest(string testName, DateTime? startTime, int timeMinutes, bool visible)
        {
            _server.SaveTestSettings(testName, startTime, timeMinutes, visible);
            LoadTests();
        }

        public void RemoveSettingsForTest(string testName)
        {
            _server.RemoveTestSettings(testName);
            LoadTests();
        }

        private void StartServer()
        {
            _server = new Server(ServerPort);
            _server.ResultSubmitted += OnResultSubmitted;
            Thread serverThread = new Thread(() =>
            {
                try
                {
                    _server.Start();
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось запустить сервер: " + ex.Message);
                }
            });
            serverThread.IsBackground = true;
            serverThread.Start();
            IsServerRunning = true;
            ServerStatusText = "Сервер запущен";
        }

        private void StartWebPanel()
        {
            _webApp = WebPanelHost.Build(AppDomain.CurrentDomain.BaseDirectory);
            _webApp.Urls.Add(PanelUrl);
            Thread webThread = new Thread(() =>
            {
                try
                {
                    _webApp.Run();
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось запустить веб-панель: " + ex.Message);
                }
            });
            webThread.IsBackground = true;
            webThread.Start();
        }

        private void OnResultSubmitted(TestResult result, string fileName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Results.Insert(0, ToRow(result, fileName));
                RefreshStats();
            });
        }

        private void OpenPanelDelayed()
        {
            Task.Run(async () =>
            {
                await Task.Delay(1500);
                if (_isClosing)
                {
                    return;
                }
                Application.Current.Dispatcher.Invoke(OpenPanel);
            });
        }

        private void ShowAddresses()
        {
            _ipList = GetLocalIps();
            if (_ipList.Count == 0)
            {
                _ipList.Add("127.0.0.1");
            }
            string ipText = "";
            for (int i = 0; i < _ipList.Count; i++)
            {
                if (i > 0)
                {
                    ipText += "\n";
                }
                ipText += _ipList[i];
            }
            IpAddressText = ipText;
            PortText = ServerPort.ToString();
        }

        private List<string> GetLocalIps()
        {
            List<string> ipList = new List<string>();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                NetworkInterface ni = interfaces[i];
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                {
                    continue;
                }

                IPInterfaceProperties props = ni.GetIPProperties();
                UnicastIPAddressInformationCollection addresses = props.UnicastAddresses;
                foreach (UnicastIPAddressInformation addr in addresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipList.Add(addr.Address.ToString());
                    }
                }
            }
            return ipList;
        }

        private async Task CopyAddressAsync()
        {
            try
            {
                Clipboard.SetText(_ipList[0] + "\n" + ServerPort);
                CopyButtonText = "Скопировано ✓";
            }
            catch
            {
                CopyButtonText = "Не удалось";
            }
            await Task.Delay(2000);
            CopyButtonText = "Копировать";
        }

        private void OpenPanel()
        {
            Process.Start(new ProcessStartInfo(PanelUrl) { UseShellExecute = true });
        }

        private void LoadTests()
        {
            Tests.Clear();
            Directory.CreateDirectory(_server.TestsFolderPath);
            Dictionary<string, TestSettings> settings = _server.GetTestSettings();
            string[] files = Directory.GetFiles(_server.TestsFolderPath, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "test_settings.json")
                {
                    continue;
                }

                TestInfo info = new TestInfo();
                info.Name = Path.GetFileNameWithoutExtension(files[i]);

                try
                {
                    string json = File.ReadAllText(files[i], Encoding.UTF8);
                    JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(json, options);
                    if (questions != null)
                    {
                        info.QuestionCount = questions.Count;
                    }
                }
                catch
                {
                    info.QuestionCount = 0;
                }

                TestSettings? settingsItem;
                if (settings.TryGetValue(info.Name, out settingsItem))
                {
                    if (!settingsItem.Visible)
                    {
                        info.StatusText = "Скрыт";
                        info.StatusLevel = 2;
                    }
                    else if (settingsItem.StartTime != null && settingsItem.StartTime.Value > DateTime.Now)
                    {
                        info.StatusText = "Запланирован";
                        info.StatusLevel = 1;
                    }
                    else
                    {
                        info.StatusText = "Доступен";
                        info.StatusLevel = 0;
                    }

                    string details = "";
                    if (settingsItem.StartTime != null)
                    {
                        details = "Начало: " + settingsItem.StartTime.Value.ToString("dd.MM HH:mm");
                    }
                    if (settingsItem.TimeMinutes > 0)
                    {
                        if (details.Length > 0)
                        {
                            details += " · ";
                        }
                        details += "Лимит: " + settingsItem.TimeMinutes + " мин";
                    }
                    info.DetailsText = details;
                }

                Tests.Add(info);
            }
            TestsInfoText = "Тестов: " + Tests.Count;
        }

        private void LoadResults()
        {
            Results.Clear();
            List<ResultRow> list = new List<ResultRow>();
            using (AppDbContext db = new AppDbContext(_server.DbPath))
            {
                foreach (ResultEntity entity in db.Results)
                {
                    list.Add(ToRow(entity));
                }
            }
            list.Sort((a, b) => b.Date.CompareTo(a.Date));
            for (int i = 0; i < list.Count; i++)
            {
                Results.Add(list[i]);
            }
            RefreshStats();
        }

        private ResultRow ToRow(TestResult result, string fileName)
        {
            ResultRow row = new ResultRow();
            row.StudentName = result.StudentName;
            row.Group = result.Group;
            row.TestName = result.TestName;
            row.Date = result.Date;
            row.Score = result.Score;
            row.CorrectAnswers = result.CorrectAnswers;
            row.TotalQuestions = result.TotalQuestions;
            row.FileName = fileName;
            row.Answers = new List<StudentAnswer>(result.Answers);
            return row;
        }

        private ResultRow ToRow(ResultEntity entity)
        {
            ResultRow row = new ResultRow();
            row.StudentName = entity.StudentName;
            row.Group = entity.Group;
            row.TestName = entity.TestName;
            row.Date = entity.Date;
            row.Score = entity.Score;
            row.CorrectAnswers = entity.CorrectAnswers;
            row.TotalQuestions = entity.TotalQuestions;
            row.FileName = entity.SourceFile;

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<StudentAnswer>? answers = JsonSerializer.Deserialize<List<StudentAnswer>>(entity.AnswersJson, options);
                if (answers != null)
                {
                    row.Answers = answers;
                }
            }
            catch
            {
                row.Answers = new List<StudentAnswer>();
            }
            return row;
        }

        private void BuildAnswerRows(ResultRow? row)
        {
            SelectedAnswers.Clear();
            if (row == null)
            {
                return;
            }

            List<Question>? questions = null;
            try
            {
                string path = Path.Combine(_server.TestsFolderPath, row.TestName + ".json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    questions = JsonSerializer.Deserialize<List<Question>>(json, options);
                }
            }
            catch
            {
                questions = null;
            }

            for (int i = 0; i < row.Answers.Count; i++)
            {
                StudentAnswer answer = row.Answers[i];
                AnswerRow item = new AnswerRow();
                item.Number = i + 1;
                item.QuestionText = answer.QuestionText;
                item.IsCorrect = answer.IsCorrect;

                Question? question = null;
                if (questions != null && questions.Count == row.Answers.Count)
                {
                    question = questions[i];
                }

                if (answer.QuestionType == QuestionTypes.Text)
                {
                    item.YourAnswer = answer.TextAnswer.Length > 0 ? answer.TextAnswer : "(нет ответа)";
                    if (answer.AcceptableAnswers.Count > 0)
                    {
                        item.CorrectAnswer = string.Join(", ", answer.AcceptableAnswers);
                    }
                    else
                    {
                        item.CorrectAnswer = "(не указан)";
                    }
                }
                else if (answer.QuestionType == QuestionTypes.Multiple)
                {
                    item.YourAnswer = FormatIndices(answer.SelectedIndices, question != null ? question.Options : null);
                    if (question != null && question.CorrectAnswerIndices.Count > 0)
                    {
                        item.CorrectAnswer = FormatIndices(question.CorrectAnswerIndices, question.Options);
                    }
                    else
                    {
                        item.CorrectAnswer = FormatIndices(new List<int> { answer.CorrectIndex }, question != null ? question.Options : null);
                    }
                }
                else
                {
                    item.YourAnswer = FormatIndices(new List<int> { answer.SelectedIndex }, question != null ? question.Options : null);
                    item.CorrectAnswer = FormatIndices(new List<int> { answer.CorrectIndex }, question != null ? question.Options : null);
                }

                SelectedAnswers.Add(item);
            }
        }

        private string FormatIndices(List<int> indices, List<string>? options)
        {
            if (indices == null || indices.Count == 0)
            {
                return "(нет ответа)";
            }
            if (indices.Count == 1 && indices[0] < 0)
            {
                return "(нет ответа)";
            }

            string text = "";
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                string part;
                if (options != null && index >= 0 && index < options.Count)
                {
                    part = (char)(1040 + index) + ") " + options[index];
                }
                else if (index >= 0)
                {
                    part = "вариант " + (index + 1);
                }
                else
                {
                    part = "(нет)";
                }
                if (i > 0)
                {
                    text += ", ";
                }
                text += part;
            }
            return text;
        }

        private void DeleteResult()
        {
            if (SelectedResult == null)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show("Удалить результат «" + SelectedResult.StudentName + " — " + SelectedResult.TestName + "»?", "SkillChecker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            string filePath = Path.Combine(_server.ResultsFolderPath, SelectedResult.FileName);
            if (SelectedResult.FileName.Length > 0 && File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (AppDbContext db = new AppDbContext(_server.DbPath))
            {
                ResultEntity? target = null;
                foreach (ResultEntity entity in db.Results)
                {
                    if (entity.SourceFile == SelectedResult.FileName)
                    {
                        target = entity;
                        break;
                    }
                }
                if (target != null)
                {
                    db.Results.Remove(target);
                    db.SaveChanges();
                }
            }

            Results.Remove(SelectedResult);
            SelectedResult = null;
            RefreshStats();
        }

        private bool FilterResult(object item)
        {
            ResultRow? row = item as ResultRow;
            if (row == null)
            {
                return false;
            }
            if (_searchText.Length == 0)
            {
                return true;
            }
            if (row.StudentName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            if (row.Group.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            if (row.TestName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return false;
        }

        private void SortBy(string field)
        {
            if (_sortField == field)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortField = field;
                _sortAscending = false;
            }
            ApplySort();
        }

        private void ApplySort()
        {
            ResultsView.SortDescriptions.Clear();
            ListSortDirection direction = _sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;

            if (_sortField == "date")
            {
                ResultsView.SortDescriptions.Add(new SortDescription("Date", direction));
            }
            else
            {
                ResultsView.SortDescriptions.Add(new SortDescription("Score", direction));
            }

            if (_sortField == "date")
            {
                DateSortLabel = _sortAscending ? "По дате ▲" : "По дате ▼";
                ScoreSortLabel = "По баллу";
            }
            else
            {
                DateSortLabel = "По дате";
                ScoreSortLabel = _sortAscending ? "По баллу ▲" : "По баллу ▼";
            }
        }

        private void RefreshStats()
        {
            List<ResultRow> visible = new List<ResultRow>();
            foreach (object item in ResultsView)
            {
                ResultRow? row = item as ResultRow;
                if (row != null)
                {
                    visible.Add(row);
                }
            }

            if (Results.Count == 0)
            {
                EmptyText = "Пока никто не сдал";
            }
            else if (visible.Count == 0)
            {
                EmptyText = "Ничего не найдено";
            }
            else
            {
                EmptyText = "";
            }

            if (visible.Count == 0)
            {
                TotalValue = "0";
                AverageValue = "—";
                BestValue = "—";
                WorstValue = "—";
                return;
            }

            double sum = 0;
            double best = visible[0].Score;
            double worst = visible[0].Score;
            for (int i = 0; i < visible.Count; i++)
            {
                double score = visible[i].Score;
                sum += score;
                if (score > best)
                {
                    best = score;
                }
                if (score < worst)
                {
                    worst = score;
                }
            }

            TotalValue = visible.Count.ToString();
            AverageValue = (sum / visible.Count).ToString("0.#") + "%";
            BestValue = best.ToString("0.#") + "%";
            WorstValue = worst.ToString("0.#") + "%";
        }

        private void ExportResults()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel (*.xlsx)|*.xlsx";
            dialog.FileName = "results.xlsx";
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            List<TestResult> list = new List<TestResult>();
            using (AppDbContext db = new AppDbContext(_server.DbPath))
            {
                foreach (ResultEntity entity in db.Results)
                {
                    TestResult result = new TestResult();
                    result.StudentName = entity.StudentName;
                    result.Group = entity.Group;
                    result.TestName = entity.TestName;
                    result.Date = entity.Date;
                    result.Score = entity.Score;
                    result.CorrectAnswers = entity.CorrectAnswers;
                    result.TotalQuestions = entity.TotalQuestions;
                    list.Add(result);
                }
            }

            if (list.Count == 0)
            {
                MessageBox.Show("Нет результатов для экспорта", "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            byte[] bytes = ExcelExportService.BuildExcel(list);
            File.WriteAllBytes(dialog.FileName, bytes);
            MessageBox.Show("Сохранено: " + dialog.FileName, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearResults()
        {
            MessageBoxResult answer = MessageBox.Show("Удалить все результаты? Действие необратимо.", "SkillChecker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            string folder = _server.ResultsFolderPath;
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.json");
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }

            using (AppDbContext db = new AppDbContext(_server.DbPath))
            {
                List<ResultEntity> entities = new List<ResultEntity>();
                foreach (ResultEntity entity in db.Results)
                {
                    entities.Add(entity);
                }
                db.Results.RemoveRange(entities);
                db.SaveChanges();
            }

            Results.Clear();
            RefreshStats();
        }

        private void DeleteTest()
        {
            if (SelectedTest == null)
            {
                return;
            }
            MessageBoxResult answer = MessageBox.Show("Удалить тест «" + SelectedTest.Name + "»?", "SkillChecker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            string filePath = Path.Combine(_server.TestsFolderPath, SelectedTest.Name + ".json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            _server.RemoveTestSettings(SelectedTest.Name);
            _server.LoadAllTests();
            LoadTests();
        }

        private void DuplicateTest()
        {
            if (SelectedTest == null)
            {
                return;
            }
            string source = Path.Combine(_server.TestsFolderPath, SelectedTest.Name + ".json");
            if (!File.Exists(source))
            {
                return;
            }

            string newName = SelectedTest.Name + "_copy";
            int counter = 2;
            while (File.Exists(Path.Combine(_server.TestsFolderPath, newName + ".json")))
            {
                newName = SelectedTest.Name + "_copy" + counter;
                counter++;
            }

            File.Copy(source, Path.Combine(_server.TestsFolderPath, newName + ".json"));
            _server.LoadAllTests();
            LoadTests();
        }

        private void ImportTest()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON (*.json)|*.json";
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(dialog.FileName);
            string target = Path.Combine(_server.TestsFolderPath, name + ".json");
            File.Copy(dialog.FileName, target, true);
            _server.LoadAllTests();
            LoadTests();
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
