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
        private string _totalText = "Сдало: 0";
        private string _averageText = "Средний балл: —";
        private string _bestText = "Лучший: —";
        private string _worstText = "Худший: —";
        private TestInfo? _selectedTest;

        public ObservableCollection<TestInfo> Tests { get; } = new ObservableCollection<TestInfo>();
        public ObservableCollection<ResultRow> Results { get; } = new ObservableCollection<ResultRow>();

        public RelayCommand CopyAddressCommand { get; }
        public RelayCommand OpenPanelCommand { get; }
        public RelayCommand CreateTestCommand { get; }
        public RelayCommand EditTestCommand { get; }
        public RelayCommand DuplicateTestCommand { get; }
        public RelayCommand DeleteTestCommand { get; }
        public RelayCommand ImportTestCommand { get; }
        public RelayCommand RefreshTestsCommand { get; }
        public RelayCommand RefreshResultsCommand { get; }
        public RelayCommand ExportResultsCommand { get; }
        public RelayCommand ClearResultsCommand { get; }

        public event Action<string?>? OpenEditorRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        public TeacherViewModel()
        {
            CopyAddressCommand = new RelayCommand(async p => await CopyAddressAsync());
            OpenPanelCommand = new RelayCommand(p => OpenPanel());
            CreateTestCommand = new RelayCommand(p => OpenEditorRequested?.Invoke(null));
            EditTestCommand = new RelayCommand(p => OpenEditorRequested?.Invoke(SelectedTest?.Name), p => SelectedTest != null);
            DuplicateTestCommand = new RelayCommand(p => DuplicateTest(), p => SelectedTest != null);
            DeleteTestCommand = new RelayCommand(p => DeleteTest(), p => SelectedTest != null);
            ImportTestCommand = new RelayCommand(p => ImportTest());
            RefreshTestsCommand = new RelayCommand(p => LoadTests());
            RefreshResultsCommand = new RelayCommand(p => LoadResults());
            ExportResultsCommand = new RelayCommand(p => ExportResults(), p => Results.Count > 0);
            ClearResultsCommand = new RelayCommand(p => ClearResults(), p => Results.Count > 0);

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

        public string TotalText
        {
            get => _totalText;
            set { _totalText = value; OnPropertyChanged(); }
        }

        public string AverageText
        {
            get => _averageText;
            set { _averageText = value; OnPropertyChanged(); }
        }

        public string BestText
        {
            get => _bestText;
            set { _bestText = value; OnPropertyChanged(); }
        }

        public string WorstText
        {
            get => _worstText;
            set { _worstText = value; OnPropertyChanged(); }
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

        private void OnResultSubmitted(TestResult result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Results.Insert(0, ToRow(result));
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

        private ResultRow ToRow(TestResult result)
        {
            ResultRow row = new ResultRow();
            row.StudentName = result.StudentName;
            row.Group = result.Group;
            row.TestName = result.TestName;
            row.Date = result.Date;
            row.Score = result.Score;
            row.CorrectAnswers = result.CorrectAnswers;
            row.TotalQuestions = result.TotalQuestions;
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
            return row;
        }

        private void RefreshStats()
        {
            if (Results.Count == 0)
            {
                TotalText = "Сдало: 0";
                AverageText = "Средний балл: —";
                BestText = "Лучший: —";
                WorstText = "Худший: —";
                return;
            }

            double sum = 0;
            double best = Results[0].Score;
            double worst = Results[0].Score;
            for (int i = 0; i < Results.Count; i++)
            {
                double score = Results[i].Score;
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

            TotalText = "Сдало: " + Results.Count;
            AverageText = "Средний балл: " + (sum / Results.Count).ToString("0.#") + "%";
            BestText = "Лучший: " + best.ToString("0.#") + "%";
            WorstText = "Худший: " + worst.ToString("0.#") + "%";
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
