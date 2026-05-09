using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;

namespace SkillCheckerServer
{
    internal partial class Server
    {
        private string ProcessCommand(string command, string[] parts, string clientEndPoint)
        {
            LoadSettings();

            if (command == Commands.GetTests)
            {
                ReloadTestsIfNeeded();
                string testNames = "";
                foreach (KeyValuePair<string, List<Question>> kvp in _tests)
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
                foreach (KeyValuePair<string, TestSettings> kvp in _testSettings)
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
                List<string> textAnswers = new List<string>();

                string[] answerParts = answersStr.Split(',');
                for (int i = 0; i < answerParts.Length; i++)
                {
                    List<int> questionAnswers = new List<int>();
                    string textAnswer = "";
                    if (ProtocolHelper.IsEncodedTextAnswer(answerParts[i]))
                    {
                        textAnswer = ProtocolHelper.DecodeTextAnswer(answerParts[i]);
                    }
                    else
                    {
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
                    }
                    selectedAnswers.Add(questionAnswers);
                    textAnswers.Add(textAnswer);
                }

                TestResult result = CalculateResult(studentName, group, testName, questions, selectedAnswers, textAnswers);
                _results.Add(result);

                LogSuccess("Результат: " + studentName + " (" + group + ") — " + result.Score + "% (" + result.CorrectAnswers + "/" + result.TotalQuestions + ") " + testName);

                SaveResultToFile(result);

                string correctIndices = "";
                for (int i = 0; i < questions.Count; i++)
                {
                    if (i > 0) correctIndices += ",";
                    if (questions[i].Type == "Text")
                    {
                        correctIndices += ProtocolHelper.EncodeAcceptableAnswers(questions[i].AcceptableAnswers);
                    }
                    else if (questions[i].Type == "Multiple" && questions[i].CorrectAnswerIndices.Count > 0)
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
    }
}
