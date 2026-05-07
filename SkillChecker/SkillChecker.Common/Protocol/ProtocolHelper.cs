using System.Text;
using System.Text.Json;

namespace SkillChecker.Common.Protocol
{
    public static class ProtocolHelper
    {
        private static readonly char Separator = '|';

        public static string BuildMessage(string command, params string[] parts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(command);
            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append(Separator);
                sb.Append(parts[i]);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        public static string[] ParseMessage(string message)
        {
            string trimmed = message.TrimEnd('\n', '\r');
            return trimmed.Split(Separator);
        }

        public static string SerializeQuestionsWithoutAnswers(List<Models.Question> questions)
        {
            List<Models.QuestionView> safeList = new List<Models.QuestionView>();
            for (int i = 0; i < questions.Count; i++)
            {
                Models.Question q = questions[i];
                Models.QuestionView qv = new Models.QuestionView();
                qv.Text = q.Text;
                qv.Options = q.Options;
                qv.Type = q.Type;
                safeList.Add(qv);
            }
            return JsonSerializer.Serialize(safeList);
        }

        public static List<Models.Question> DeserializeQuestions(string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            List<Models.Question>? list = JsonSerializer.Deserialize<List<Models.Question>>(json, options);
            return list ?? new List<Models.Question>();
        }
    }
}
