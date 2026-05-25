using System.Net.Sockets;
using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;
using SkillChecker.Models;
using Cmd = SkillChecker.Common.Protocol.Commands;

namespace SkillChecker.Services
{
    public class ClientService
    {
        private string _serverIp;
        private int _serverPort;

        public ClientService()
        {
            _serverIp = "127.0.0.1";
            _serverPort = 9000;
        }

        public string ServerIp { get => _serverIp; set => _serverIp = value; }
        public int ServerPort { get => _serverPort; set => _serverPort = value; }

        public List<string> GetTestList()
        {
            string response = Send(ProtocolHelper.BuildMessage(Cmd.GetTests));
            string[] parts = ProtocolHelper.ParseMessage(response);

            if (parts.Length >= 2 && parts[0] == Cmd.TestsList)
            {
                List<string> tests = new List<string>();
                for (int i = 1; i < parts.Length; i++)
                {
                    string name = parts[i].Trim();
                    if (name.Length > 0)
                    {
                        tests.Add(name);
                    }
                }
                return tests;
            }

            return new List<string>();
        }

        public TestQuestionsResult GetTestQuestions(string testName)
        {
            string response = Send(ProtocolHelper.BuildMessage(Cmd.GetTest, testName));
            string[] parts = ProtocolHelper.ParseMessage(response);

            TestQuestionsResult result = new TestQuestionsResult();

            if (parts.Length >= 2 && parts[0] == Cmd.TestData)
            {
                result.Questions = ProtocolHelper.DeserializeQuestions(parts[1]);
                if (parts.Length >= 3 && int.TryParse(parts[2], out int timeMinutes))
                {
                    result.TimeMinutes = timeMinutes;
                }
                return result;
            }

            if (parts.Length >= 2 && parts[0] == Cmd.StartWait)
            {
                result.IsWaiting = true;
                if (DateTime.TryParse(parts[1], out DateTime dt))
                {
                    result.WaitTime = dt;
                }
                if (parts.Length >= 3 && int.TryParse(parts[2], out int timeMinutes))
                {
                    result.TimeMinutes = timeMinutes;
                }
                return result;
            }

            throw new Exception("Не удалось получить тест");
        }

        public List<ScheduledTest> GetScheduledTests()
        {
            string response = Send(ProtocolHelper.BuildMessage(Cmd.GetTestSettings));
            string[] parts = ProtocolHelper.ParseMessage(response);

            List<ScheduledTest> list = new List<ScheduledTest>();

            if (parts.Length >= 2 && parts[0] == Cmd.TestSettingsList)
            {
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    ScheduledTest st = new ScheduledTest();
                    st.Name = parts[i];
                    string startTimeStr = parts[i + 1];
                    if (startTimeStr.Length > 0 && DateTime.TryParse(startTimeStr, out DateTime dt))
                    {
                        st.ScheduledTime = dt;
                    }
                    if (int.TryParse(parts[i + 2], out int timeMinutes))
                    {
                        st.TimeMinutes = timeMinutes;
                    }
                    list.Add(st);
                }
            }

            return list;
        }

        public TestResult SubmitAnswers(string studentName, string group, string testName, List<List<int>> answers, List<string> textAnswers)
        {
            string answersStr = "";
            for (int i = 0; i < answers.Count; i++)
            {
                if (i > 0) answersStr += ",";
                string textAt = textAnswers != null && i < textAnswers.Count ? textAnswers[i] : "";
                if (textAt != null && textAt.Length > 0)
                {
                    answersStr += ProtocolHelper.EncodeTextAnswer(textAt);
                }
                else if (answers[i].Count > 0)
                {
                    for (int j = 0; j < answers[i].Count; j++)
                    {
                        if (j > 0) answersStr += ";";
                        answersStr += answers[i][j].ToString();
                    }
                }
                else
                {
                    answersStr += "-1";
                }
            }

            string response = Send(ProtocolHelper.BuildMessage(Cmd.SubmitAnswers, studentName, group, testName, answersStr));
            string[] parts = ProtocolHelper.ParseMessage(response);

            TestResult result = new TestResult();
            if (parts.Length >= 5 && parts[0] == Cmd.Result)
            {
                double score;
                int correct, total;
                if (double.TryParse(parts[1], out score)) result.Score = score;
                if (int.TryParse(parts[2], out correct)) result.CorrectAnswers = correct;
                if (int.TryParse(parts[3], out total)) result.TotalQuestions = total;

                string[] correctParts = parts[4].Split(',');
                for (int i = 0; i < correctParts.Length; i++)
                {
                    StudentAnswer sa = new StudentAnswer();

                    if (ProtocolHelper.IsEncodedTextAnswer(correctParts[i]))
                    {
                        sa.QuestionType = QuestionTypes.Text;
                        sa.AcceptableAnswers = ProtocolHelper.DecodeAcceptableAnswers(correctParts[i]);
                        sa.CorrectIndex = -1;
                        sa.SelectedIndex = -1;
                        if (textAnswers != null && i < textAnswers.Count)
                        {
                            sa.TextAnswer = textAnswers[i];
                        }
                        sa.IsCorrect = AnswerChecker.CheckTextAnswer(sa.TextAnswer, sa.AcceptableAnswers);
                    }
                    else
                    {
                        List<int> correctIndices = new List<int>();
                        string[] subParts = correctParts[i].Split(';');
                        for (int j = 0; j < subParts.Length; j++)
                        {
                            if (int.TryParse(subParts[j], out int idx))
                            {
                                correctIndices.Add(idx);
                            }
                        }

                        if (correctIndices.Count > 1)
                        {
                            sa.QuestionType = QuestionTypes.Multiple;
                        }
                        sa.CorrectIndex = correctIndices.Count > 0 ? correctIndices[0] : 0;

                        if (i < answers.Count && answers[i].Count > 0)
                        {
                            sa.SelectedIndex = answers[i][0];
                            sa.SelectedIndices = new List<int>(answers[i]);
                            if (answers[i].Count > 1)
                            {
                                sa.QuestionType = QuestionTypes.Multiple;
                            }
                        }
                        else
                        {
                            sa.SelectedIndex = -1;
                        }

                        sa.IsCorrect = AnswerChecker.CheckAnswer(
                            sa.SelectedIndices.Count > 0 ? sa.SelectedIndices : new List<int>(),
                            correctIndices,
                            sa.QuestionType);
                    }

                    result.Answers.Add(sa);
                }
            }
            return result;
        }

        private string Send(string message)
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(_serverIp, _serverPort);

                using (NetworkStream stream = client.GetStream())
                {
                    ProtocolFramer.WriteFrame(stream, message);
                    return ProtocolFramer.ReadFrame(stream);
                }
            }
        }
    }
}
