using System.Net;
using System.Net.Sockets;
using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;
using SkillChecker.Data;

namespace SkillCheckerServer
{
    internal partial class Server
    {
        private TcpListener _listener;
        private bool _isRunning;
        private Dictionary<string, List<Question>> _tests;
        private Dictionary<string, TestSettings> _testSettings;
        private List<TestResult> _results;
        private string _testsFolder;
        private string _settingsFile;
        private string _dbPath;
        private readonly object _stateLock = new object();

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _isRunning = false;
            _tests = new Dictionary<string, List<Question>>();
            _testSettings = new Dictionary<string, TestSettings>();
            _results = new List<TestResult>();
            _testsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests");
            string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            string sourceTestsFolder = Path.Combine(solutionDir, "SkillCheckerServer", "Tests");
            if (Directory.Exists(sourceTestsFolder))
            {
                _testsFolder = sourceTestsFolder;
            }
            _settingsFile = Path.Combine(_testsFolder, "test_settings.json");
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "skillchecker.db");
            if (Directory.Exists(sourceTestsFolder))
            {
                _dbPath = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "skillchecker.db");
            }
        }

        public void Start()
        {
            Directory.CreateDirectory(_testsFolder);
            LoadAllTests();
            LoadSettings();
            InitializeDatabase();

            _isRunning = true;
            _listener.Start();
            Log("Сервер запущен на порту " + ((IPEndPoint)_listener.LocalEndpoint).Port);
            Log("Папка тестов: " + _testsFolder);
            Log("Ожидание подключений...");

            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread thread = new Thread(() => HandleClient(client));
                    thread.IsBackground = true;
                    thread.Start();
                }
                catch
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Log("Сервер остановлен.");
        }

        private void HandleClient(TcpClient client)
        {
            string endPoint = client.Client.RemoteEndPoint?.ToString() ?? "?";

            try
            {
                client.ReceiveTimeout = 60000;
                client.SendTimeout = 30000;
                using (NetworkStream stream = client.GetStream())
                {
                    while (client.Connected)
                    {
                        string line;
                        try
                        {
                            line = ProtocolFramer.ReadFrame(stream);
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                        catch (ProtocolException ex)
                        {
                            LogError("Некорректный фрейм от " + endPoint + ": " + ex.Message);
                            break;
                        }

                        string[] parts = ProtocolHelper.ParseMessage(line);
                        string command = parts[0];

                        if (command == Commands.GetTest || command == Commands.CheckStart)
                        {
                            LogInfo("Запрос теста от " + endPoint);
                        }

                        string response = ProcessCommand(command, parts, endPoint);
                        ProtocolFramer.WriteFrame(stream, response);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка клиента " + endPoint + ": " + ex.Message);
            }
        }

        public List<string> GetTestNames()
        {
            lock (_stateLock)
            {
                List<string> names = new List<string>();
                foreach (KeyValuePair<string, List<Question>> kvp in _tests)
                {
                    if (_testSettings.ContainsKey(kvp.Key) && !_testSettings[kvp.Key].Visible)
                    {
                        continue;
                    }
                    names.Add(kvp.Key);
                }
                return names;
            }
        }

        private void Log(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
        }

        private void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }

        private void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }

        private void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }
    }
}
