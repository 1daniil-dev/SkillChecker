using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;

namespace SkillCheckerServer
{
    internal partial class Server
    {
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
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Dictionary<string, JsonElement>? data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
                if (data != null)
                {
                    foreach (KeyValuePair<string, JsonElement> kvp in data)
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
            foreach (KeyValuePair<string, TestSettings> kvp in _testSettings)
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

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_settingsFile, json, Encoding.UTF8);
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
    }
}
