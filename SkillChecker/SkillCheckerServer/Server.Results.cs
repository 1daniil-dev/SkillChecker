using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;

namespace SkillCheckerServer
{
    internal partial class Server
    {
        private TestResult CalculateResult(string studentName, string group, string testName, List<Question> questions, List<List<int>> selectedAnswers, List<string> textAnswers)
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

                if (questions[i].Type == QuestionTypes.Text)
                {
                    string typed = i < textAnswers.Count ? textAnswers[i] : "";
                    answer.TextAnswer = typed;
                    answer.AcceptableAnswers = questions[i].AcceptableAnswers;
                    answer.SelectedIndex = -1;
                    answer.IsCorrect = AnswerChecker.CheckTextAnswer(typed, questions[i].AcceptableAnswers);
                    if (answer.IsCorrect) correctCount++;
                }
                else if (i < selectedAnswers.Count && selectedAnswers[i].Count > 0)
                {
                    if (questions[i].Type == QuestionTypes.Multiple)
                    {
                        answer.SelectedIndices = selectedAnswers[i];
                        answer.SelectedIndex = selectedAnswers[i][0];

                        answer.IsCorrect = AnswerChecker.CheckAnswer(selectedAnswers[i], questions[i].CorrectAnswerIndices, QuestionTypes.Multiple);
                        if (answer.IsCorrect) correctCount++;
                    }
                    else
                    {
                        answer.SelectedIndex = selectedAnswers[i][0];
                        answer.IsCorrect = AnswerChecker.CheckAnswer(selectedAnswers[i], new List<int> { questions[i].CorrectAnswerIndex }, QuestionTypes.Single);
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

        private string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                bool isInvalid = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (name[i] == invalid[j])
                    {
                        isInvalid = true;
                        break;
                    }
                }
                sb.Append(isInvalid ? '_' : name[i]);
            }
            return sb.ToString();
        }

        private void SaveResultToFile(TestResult result)
        {
            string resultsFolder = Path.Combine(Path.GetDirectoryName(_testsFolder) ?? "", "Results");
            Directory.CreateDirectory(resultsFolder);

            string safeName = SanitizeFileName(result.StudentName.Replace(" ", "_"));
            string safeTest = SanitizeFileName(result.TestName);
            string fileName = "result_" + safeName + "_" + safeTest + "_" + result.Date.ToString("yyyyMMdd_HHmm") + ".json";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(result, options);
            File.WriteAllText(Path.Combine(resultsFolder, fileName), json, Encoding.UTF8);
        }

        public void ShowResults()
        {
            lock (_stateLock)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  Результаты:");
                Console.ResetColor();

                if (_results.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("    Пока нет результатов");
                    Console.ResetColor();
                    return;
                }

                for (int i = 0; i < _results.Count; i++)
                {
                    TestResult r = _results[i];
                    string num = (i + 1).ToString();
                    string line = "    " + num + ". " + r.StudentName + " (" + r.Group + ") — "
                        + r.TestName + " — " + r.Score + "% (" + r.CorrectAnswers + "/" + r.TotalQuestions + ") "
                        + r.Date.ToString("HH:mm");

                    if (r.Score >= 75)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (r.Score >= 50)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine(line);
                    Console.ResetColor();
                }
            }
        }
    }
}
