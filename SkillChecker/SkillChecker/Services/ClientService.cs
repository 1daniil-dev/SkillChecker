using System.IO;
using System.Net.Sockets;
using System.Text;
using SkillChecker.Common.Models;
using SkillChecker.Common.Protocol;
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
                string[] names = parts[1].Split('|');
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i].Trim();
                    if (name.Length > 0)
                    {
                        tests.Add(name);
                    }
                }
                return tests;
            }

            return new List<string>();
        }

        public List<Question> GetTestQuestions(string testName)
        {
            string response = Send(ProtocolHelper.BuildMessage(Cmd.GetTest, testName));
            string[] parts = ProtocolHelper.ParseMessage(response);

            if (parts.Length >= 2 && parts[0] == Cmd.TestData)
            {
                return ProtocolHelper.DeserializeQuestions(parts[1]);
            }

            if (parts.Length >= 2 && parts[0] == Cmd.StartWait)
            {
                throw new Exception("Тест начнётся в " + parts[1] + ". Подождите.");
            }

            throw new Exception("Не удалось получить тест");
        }

        public TestResult SubmitAnswers(string studentName, string group, string testName, List<int> answers)
        {
            string answersStr = "";
            for (int i = 0; i < answers.Count; i++)
            {
                if (i > 0) answersStr += ",";
                answersStr += answers[i].ToString();
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
                    if (int.TryParse(correctParts[i], out int idx)) sa.CorrectIndex = idx;
                    if (i < answers.Count) sa.SelectedIndex = answers[i];
                    sa.IsCorrect = sa.SelectedIndex == sa.CorrectIndex;
                    result.Answers.Add(sa);
                }
            }
            return result;
        }

        private string Send(string message)
        {
            if (!message.EndsWith("\n"))
            {
                message += "\n";
            }

            using (TcpClient client = new TcpClient())
            {
                client.Connect(_serverIp, _serverPort);

                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    writer.Write(message);
                    return reader.ReadLine() ?? "";
                }
            }
        }
    }
}
