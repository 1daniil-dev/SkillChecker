using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;

namespace SkillCheckerServer
{
    class Server
    {
        private TcpListener _listener;
        private bool _isRunning;
        private Dictionary<string, List<Question>> _tests;
        private Dictionary<string, DateTime?> _schedules;
        private List<TestResult> _results;
        private string _testsFolder;

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _isRunning = false;
            _tests = new Dictionary<string, List<Question>>();
            _schedules = new Dictionary<string, DateTime?>();
            _results = new List<TestResult>();
            _testsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests");
        }

        public void Start()
        {
            Directory.CreateDirectory(_testsFolder);
            LoadAllTests();

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

        private void LoadAllTests()
        {
            _tests.Clear();

            if (!Directory.Exists(_testsFolder)) return;

            string[] files = Directory.GetFiles(_testsFolder, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string json = File.ReadAllText(files[i], Encoding.UTF8);
                    List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(json);
                    if (questions != null && questions.Count > 0)
                    {
                        string name = Path.GetFileNameWithoutExtension(files[i]);
                        _tests[name] = questions;
                        Log("Загружен тест: " + name + " (" + questions.Count + " вопросов)");
                    }
                }
                catch (Exception ex)
                {
                    Log("Ошибка загрузки " + files[i] + ": " + ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            string endPoint = client.Client.RemoteEndPoint?.ToString() ?? "?";
            Log("Подключён: " + endPoint);

            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    while (client.Connected)
                    {
                        string? line = reader.ReadLine();
                        if (line == null) break;

                        string[] parts = ProtocolHelper.ParseMessage(line);
                        string command = parts[0];

                        string response = ProcessCommand(command, parts, endPoint);
                        writer.Write(response);
                    }
                }
            }
            catch
            {
            }

            Log("Отключён: " + endPoint);
        }

        private string ProcessCommand(string command, string[] parts, string clientEndPont)
        {
            if (command == Commands.GetTests)
            {
                ReloadTestsIfNeeded();
                string testNames = "";
                foreach (var kvp in _tests)
                {
                    if (testNames.Length > 0) testNames += "|";
                    testNames += kvp.Key;
                }
                return ProtocolHelper.BuildMessage(Commands.TestsList, testNames);
            }

            if (command == Commands.GetTest && parts.Length >= 2)
            {
                string testName = parts[1];
                if (_tests.ContainsKey(testName))
                {
                    DateTime? schedule = _schedules.ContainsKey(testName) ? _schedules[testName] : null;
                    if (schedule != null && DateTime.Now < schedule.Value)
                    {
                        return ProtocolHelper.BuildMessage(Commands.StartWait, schedule.Value.ToString("HH:mm"));
                    }

                    string json = ProtocolHelper.SerializeQuestionsWithoutAnswers(_tests[testName]);
                    return ProtocolHelper.BuildMessage(Commands.TestData, json);
                }
                return ProtocolHelper.BuildMessage(Commands.Error, "Тест не найден");
            }

            if (command == Commands.CheckStart && parts.Length >= 2)
            {
                string testName = parts[1];
                DateTime? schedule = _schedules.ContainsKey(testName) ? _schedules[testName] : null;
                if (schedule == null || DateTime.Now >= schedule.Value)
                {
                    return ProtocolHelper.BuildMessage(Commands.StartOk);
                }
                return ProtocolHelper.BuildMessage(Commands.StartWait, schedule.Value.ToString("HH:mm"));
            }

            if (command == Commands.SubmitAnswers && parts.Length >= 5)
            {
                string studentName = parts[1];
                string group = parts[2];
                string testName = parts[3];
                string answersStr = parts[4];

                if (!_tests.ContainsKey(testName))
                {
                    return ProtocolHelper.BuildMessage(Commands.Error, "Тест не найден");
                }

                List<Question> questions = _tests[testName];
                List<int> selectedAnswers = new List<int>();

                string[] answerParts = answersStr.Split(',');
                for (int i = 0; i < answerParts.Length; i++)
                {
                    if (int.TryParse(answerParts[i], out int val))
                    {
                        selectedAnswers.Add(val);
                    }
                    else
                    {
                        selectedAnswers.Add(-1);
                    }
                }

                TestResult result = CalculateResult(studentName, group, testName, questions, selectedAnswers);
                _results.Add(result);

                Log("Результат: " + studentName + " (" + group + ") — " + result.Score + "% (" + result.CorrectAnswers + "/" + result.TotalQuestions + ") " + testName);

                SaveResultToFile(result);

                return ProtocolHelper.BuildMessage(Commands.Result,
                    result.Score.ToString(),
                    result.CorrectAnswers.ToString(),
                    result.TotalQuestions.ToString());
            }

            return ProtocolHelper.BuildMessage(Commands.Error, "Неизвестная команда");
        }

        private TestResult CalculateResult(string studentName, string group, string testName, List<Question> questions, List<int> selectedAnswers)
        {
            TestResult result = new TestResult();
            result.StudentName = studentName;
            result.Group = group;
            result.TestName = testName;
            result.Date = DateTime.Now;
            result.TotalQuestions = questions.Count;

            int correctCount = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                StudentAnswer answer = new StudentAnswer();
                answer.QuestionText = questions[i].Text;
                answer.CorrectIndex = questions[i].CorrectAnswerIndex;

                if (i < selectedAnswers.Count)
                {
                    answer.SelectedIndex = selectedAnswers[i];
                    answer.IsCorrect = selectedAnswers[i] == questions[i].CorrectAnswerIndex;
                    if (answer.IsCorrect) correctCount++;
                }
                else
                {
                    answer.SelectedIndex = -1;
                    answer.IsCorrect = false;
                }

                result.Answers.Add(answer);
            }

            result.CorrectAnswers = correctCount;
            if (questions.Count > 0)
            {
                result.Score = Math.Round((double)correctCount / questions.Count * 100, 1);
            }

            return result;
        }

        private void SaveResultToFile(TestResult result)
        {
            string resultsFolder = Path.Combine(_testsFolder, "..", "Results");
            Directory.CreateDirectory(resultsFolder);

            string fileName = "result_" + result.StudentName.Replace(" ", "_") + "_" + result.TestName + "_" + result.Date.ToString("yyyyMMdd_HHmm") + ".json";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(result, options);
            File.WriteAllText(Path.Combine(resultsFolder, fileName), json, Encoding.UTF8);
        }

        private void ReloadTestsIfNeeded()
        {
            if (!Directory.Exists(_testsFolder)) return;
            string[] files = Directory.GetFiles(_testsFolder, "*.json");
            if (files.Length != _tests.Count)
            {
                LoadAllTests();
            }
        }

        public void SetSchedule(string testName, string timeStr)
        {
            if (TimeOnly.TryParse(timeStr, out TimeOnly time))
            {
                DateTime scheduled = DateTime.Today.Add(time.ToTimeSpan());
                if (scheduled < DateTime.Now) scheduled = scheduled.AddDays(1);
                _schedules[testName] = scheduled;
                Log("Запланировано: " + testName + " на " + scheduled.ToString("HH:mm"));
            }
        }

        public void ShowResults()
        {
            Log("--- РЕЗУЛЬТАТЫ ---");
            for (int i = 0; i < _results.Count; i++)
            {
                TestResult r = _results[i];
                Log((i + 1) + ". " + r.StudentName + " (" + r.Group + ") " + r.TestName + " = " + r.Score + "% (" + r.CorrectAnswers + "/" + r.TotalQuestions + ") " + r.Date.ToString("HH:mm"));
            }
            Log("------------------");
        }

        private void Log(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
        }
    }
}
