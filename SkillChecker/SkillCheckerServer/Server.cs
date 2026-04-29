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
        private Dictionary<string, TestSettings> _testSettings;
        private List<TestResult> _results;
        private string _testsFolder;
        private string _settingsFile;

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _isRunning = false;
            _tests = new Dictionary<string, List<Question>>();
            _testSettings = new Dictionary<string, TestSettings>();
            _results = new List<TestResult>();
            string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            _testsFolder = Path.Combine(solutionDir, "SkillCheckerServer", "Tests");
            _settingsFile = Path.Combine(_testsFolder, "test_settings.json");
        }

        public void Start()
        {
            Directory.CreateDirectory(_testsFolder);
            LoadAllTests();
            LoadSettings();

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

        public void LoadAllTests()
        {
            _tests.Clear();

            if (!Directory.Exists(_testsFolder)) return;

            string[] files = Directory.GetFiles(_testsFolder, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "test_settings.json" || fileName == "schedule.json") continue;

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

        private void LoadSettings()
        {
            if (!File.Exists(_settingsFile)) return;

            try
            {
                string json = File.ReadAllText(_settingsFile, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Dictionary<string, JsonElement>? data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        TestSettings settings = new TestSettings();

                        if (kvp.Value.ValueKind == JsonValueKind.Object)
                        {
                            JsonElement startElem;
                            if (kvp.Value.TryGetProperty("StartTime", out startElem) && startElem.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(startElem.GetString(), out DateTime dt))
                                {
                                    settings.StartTime = dt;
                                }
                            }

                            JsonElement timeElem;
                            if (kvp.Value.TryGetProperty("TimeMinutes", out timeElem) && timeElem.ValueKind == JsonValueKind.Number)
                            {
                                settings.TimeMinutes = timeElem.GetInt32();
                            }

                            JsonElement visibleElem;
                            if (kvp.Value.TryGetProperty("Visible", out visibleElem) && (visibleElem.ValueKind == JsonValueKind.True || visibleElem.ValueKind == JsonValueKind.False))
                            {
                                settings.Visible = visibleElem.GetBoolean();
                            }
                        }
                        else if (kvp.Value.ValueKind == JsonValueKind.String)
                        {
                            if (DateTime.TryParse(kvp.Value.GetString(), out DateTime dt))
                            {
                                settings.StartTime = dt;
                            }
                        }

                        _testSettings[kvp.Key] = settings;
                    }
                    Log("Загружено настроек тестов: " + _testSettings.Count);
                }
            }
            catch (Exception ex)
            {
                Log("Ошибка загрузки настроек: " + ex.Message);
            }
        }

        private void SaveSettings()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            foreach (var kvp in _testSettings)
            {
                Dictionary<string, object> entry = new Dictionary<string, object>();
                if (kvp.Value.StartTime != null)
                {
                    entry["StartTime"] = kvp.Value.StartTime.Value.ToString("o");
                }
                else
                {
                    entry["StartTime"] = "";
                }
                entry["TimeMinutes"] = kvp.Value.TimeMinutes;
                entry["Visible"] = kvp.Value.Visible;
                data[kvp.Key] = entry;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_settingsFile, json, Encoding.UTF8);
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
            catch (Exception ex)
            {
                Log("Ошибка клиента " + endPoint + ": " + ex.Message);
            }

            Log("Отключён: " + endPoint);
        }

        private string ProcessCommand(string command, string[] parts, string clientEndPoint)
        {
            LoadSettings();

            if (command == Commands.GetTests)
            {
                ReloadTestsIfNeeded();
                string testNames = "";
                foreach (var kvp in _tests)
                {
                    if (_testSettings.ContainsKey(kvp.Key) && !_testSettings[kvp.Key].Visible)
                    {
                        continue;
                    }
                    if (testNames.Length > 0) testNames += "|";
                    testNames += kvp.Key;
                }
                return ProtocolHelper.BuildMessage(Commands.TestsList, testNames);
            }

            if (command == Commands.GetTestSettings)
            {
                ReloadTestsIfNeeded();
                LoadSettings();
                string settingsData = "";
                foreach (var kvp in _testSettings)
                {
                    if (!kvp.Value.Visible)
                    {
                        continue;
                    }
                    if (settingsData.Length > 0) settingsData += "|";
                    string startTime = kvp.Value.StartTime != null ? kvp.Value.StartTime.Value.ToString("o") : "";
                    settingsData += kvp.Key + "|" + startTime + "|" + kvp.Value.TimeMinutes.ToString();
                }
                return ProtocolHelper.BuildMessage(Commands.TestSettingsList, settingsData);
            }

            if (command == Commands.GetTest && parts.Length >= 2)
            {
                string testName = parts[1];
                if (_tests.ContainsKey(testName))
                {
                    if (_testSettings.ContainsKey(testName) && !_testSettings[testName].Visible)
                    {
                        return ProtocolHelper.BuildMessage(Commands.Error, "Тест недоступен");
                    }

                    TestSettings? settings = _testSettings.ContainsKey(testName) ? _testSettings[testName] : null;
                    if (settings != null && settings.StartTime != null && DateTime.Now < settings.StartTime.Value)
                    {
                        string timeMinutes = settings.TimeMinutes.ToString();
                        return ProtocolHelper.BuildMessage(Commands.StartWait, settings.StartTime.Value.ToString("o"), timeMinutes);
                    }

                    string json = ProtocolHelper.SerializeQuestionsWithoutAnswers(_tests[testName]);

                    int timeMinutesValue = 0;
                    if (settings != null) timeMinutesValue = settings.TimeMinutes;

                    return ProtocolHelper.BuildMessage(Commands.TestData, json, timeMinutesValue.ToString());
                }
                return ProtocolHelper.BuildMessage(Commands.Error, "Тест не найден");
            }

            if (command == Commands.CheckStart && parts.Length >= 2)
            {
                string testName = parts[1];
                if (_testSettings.ContainsKey(testName) && !_testSettings[testName].Visible)
                {
                    return ProtocolHelper.BuildMessage(Commands.Error, "Тест недоступен");
                }
                TestSettings? settings = _testSettings.ContainsKey(testName) ? _testSettings[testName] : null;
                if (settings == null || settings.StartTime == null || DateTime.Now >= settings.StartTime.Value)
                {
                    return ProtocolHelper.BuildMessage(Commands.StartOk);
                }
                string timeMinutes = settings.TimeMinutes.ToString();
                return ProtocolHelper.BuildMessage(Commands.StartWait, settings.StartTime.Value.ToString("o"), timeMinutes);
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
                List<List<int>> selectedAnswers = new List<List<int>>();

                string[] answerParts = answersStr.Split(',');
                for (int i = 0; i < answerParts.Length; i++)
                {
                    List<int> questionAnswers = new List<int>();
                    string[] subParts = answerParts[i].Split(';');
                    for (int j = 0; j < subParts.Length; j++)
                    {
                        if (int.TryParse(subParts[j], out int val))
                        {
                            questionAnswers.Add(val);
                        }
                        else
                        {
                            questionAnswers.Add(-1);
                        }
                    }
                    selectedAnswers.Add(questionAnswers);
                }

                TestResult result = CalculateResult(studentName, group, testName, questions, selectedAnswers);
                _results.Add(result);

                Log("Результат: " + studentName + " (" + group + ") — " + result.Score + "% (" + result.CorrectAnswers + "/" + result.TotalQuestions + ") " + testName);

                SaveResultToFile(result);

                string correctIndices = "";
                for (int i = 0; i < questions.Count; i++)
                {
                    if (i > 0) correctIndices += ",";
                    if (questions[i].Type == "Multiple" && questions[i].CorrectAnswerIndices.Count > 0)
                    {
                        for (int j = 0; j < questions[i].CorrectAnswerIndices.Count; j++)
                        {
                            if (j > 0) correctIndices += ";";
                            correctIndices += questions[i].CorrectAnswerIndices[j].ToString();
                        }
                    }
                    else
                    {
                        correctIndices += questions[i].CorrectAnswerIndex.ToString();
                    }
                }

                return ProtocolHelper.BuildMessage(Commands.Result,
                    result.Score.ToString(),
                    result.CorrectAnswers.ToString(),
                    result.TotalQuestions.ToString(),
                    correctIndices);
            }

            return ProtocolHelper.BuildMessage(Commands.Error, "Неизвестная команда");
        }

        private TestResult CalculateResult(string studentName, string group, string testName, List<Question> questions, List<List<int>> selectedAnswers)
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
                answer.QuestionType = questions[i].Type;

                if (i < selectedAnswers.Count && selectedAnswers[i].Count > 0)
                {
                    if (questions[i].Type == "Multiple")
                    {
                        answer.SelectedIndices = selectedAnswers[i];
                        answer.SelectedIndex = selectedAnswers[i][0];

                        List<int> correct = questions[i].CorrectAnswerIndices;
                        bool isCorrect = true;

                        if (selectedAnswers[i].Count != correct.Count)
                        {
                            isCorrect = false;
                        }
                        else
                        {
                            for (int j = 0; j < selectedAnswers[i].Count; j++)
                            {
                                bool found = false;
                                for (int k = 0; k < correct.Count; k++)
                                {
                                    if (selectedAnswers[i][j] == correct[k])
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    isCorrect = false;
                                    break;
                                }
                            }
                        }

                        answer.IsCorrect = isCorrect;
                        if (isCorrect) correctCount++;
                    }
                    else
                    {
                        answer.SelectedIndex = selectedAnswers[i][0];
                        answer.IsCorrect = selectedAnswers[i][0] == questions[i].CorrectAnswerIndex;
                        if (answer.IsCorrect) correctCount++;
                    }
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
            string solutionDir = Path.GetFullPath(Path.Combine(_testsFolder, "..", ".."));
            string resultsFolder = Path.Combine(solutionDir, "Results");
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
            int testFileCount = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName != "test_settings.json" && fileName != "schedule.json") testFileCount++;
            }
            if (testFileCount != _tests.Count)
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

                TestSettings settings;
                if (_testSettings.ContainsKey(testName))
                {
                    settings = _testSettings[testName];
                }
                else
                {
                    settings = new TestSettings();
                }
                settings.StartTime = scheduled;
                _testSettings[testName] = settings;

                SaveSettings();
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
