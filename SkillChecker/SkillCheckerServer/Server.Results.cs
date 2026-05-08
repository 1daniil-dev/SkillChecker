using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;

namespace SkillCheckerServer
{
    internal partial class Server
    {
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

                        answer.IsCorrect = AnswerChecker.CheckAnswer(selectedAnswers[i], questions[i].CorrectAnswerIndices, "Multiple");
                        if (answer.IsCorrect) correctCount++;
                    }
                    else
                    {
                        answer.SelectedIndex = selectedAnswers[i][0];
                        answer.IsCorrect = AnswerChecker.CheckAnswer(selectedAnswers[i], new List<int> { questions[i].CorrectAnswerIndex }, "Single");
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
            string resultsFolder = Path.Combine(Path.GetDirectoryName(_testsFolder) ?? "", "Results");
            Directory.CreateDirectory(resultsFolder);

            string fileName = "result_" + result.StudentName.Replace(" ", "_") + "_" + result.TestName + "_" + result.Date.ToString("yyyyMMdd_HHmm") + ".json";

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
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ┌──────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  РЕЗУЛЬТАТЫ                                             │");
            Console.WriteLine("  ├──────────────────────────────────────────────────────────┤");
            Console.ResetColor();
            for (int i = 0; i < _results.Count; i++)
            {
                TestResult r = _results[i];
                string num = (i + 1).ToString().PadLeft(3);
                string name = (r.StudentName + " (" + r.Group + ")").PadRight(25);
                string test = r.TestName.PadRight(15);
                string score = (r.Score + "%").PadLeft(7);
                string correct = (r.CorrectAnswers + "/" + r.TotalQuestions).PadLeft(5);
                string time = r.Date.ToString("HH:mm");

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
                Console.WriteLine("  │  " + num + ". " + name + " " + test + " " + score + " " + correct + "  " + time + " │");
                Console.ResetColor();
            }
            if (_results.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  │  Пока нет результатов                                  │");
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  └──────────────────────────────────────────────────────────┘");
            Console.ResetColor();
        }
    }
}
