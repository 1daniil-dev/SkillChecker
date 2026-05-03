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
            List<object> safeList = new List<object>();
            for (int i = 0; i < questions.Count; i++)
            {
                Models.Question q = questions[i];
                safeList.Add(new { Text = q.Text, Options = q.Options, Type = q.Type });
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
