using System.Text;
using System.Text.Json;

namespace SkillChecker.Common.Protocol
{
    public static class ProtocolHelper
    {
        private static readonly char Separator = '|';
        public const string TextAnswerPrefix = "T:";
        public const char TextItemSeparator = '~';

        public static string BuildMessage(string command, params string[] parts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(command);
            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append(Separator);
                sb.Append(parts[i]);
            }
            return sb.ToString();
        }

        public static string[] ParseMessage(string message)
        {
            string trimmed = message.TrimEnd('\n', '\r');
            return trimmed.Split(Separator);
        }

        public static string EncodeTextAnswer(string text)
        {
            string safe = text == null ? "" : text;
            return TextAnswerPrefix + Uri.EscapeDataString(safe);
        }

        public static string DecodeTextAnswer(string encoded)
        {
            if (encoded == null || !encoded.StartsWith(TextAnswerPrefix))
            {
                return "";
            }
            string payload = encoded.Substring(TextAnswerPrefix.Length);
            return Uri.UnescapeDataString(payload);
        }

        public static bool IsEncodedTextAnswer(string encoded)
        {
            return encoded != null && encoded.StartsWith(TextAnswerPrefix);
        }

        public static string EncodeAcceptableAnswers(List<string> acceptableAnswers)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(TextAnswerPrefix);
            if (acceptableAnswers != null)
            {
                for (int i = 0; i < acceptableAnswers.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(TextItemSeparator);
                    }
                    string item = acceptableAnswers[i] == null ? "" : acceptableAnswers[i];
                    sb.Append(Uri.EscapeDataString(item));
                }
            }
            return sb.ToString();
        }

        public static List<string> DecodeAcceptableAnswers(string encoded)
        {
            List<string> result = new List<string>();
            if (encoded == null || !encoded.StartsWith(TextAnswerPrefix))
            {
                return result;
            }
            string payload = encoded.Substring(TextAnswerPrefix.Length);
            if (payload.Length == 0)
            {
                return result;
            }
            string[] parts = payload.Split(TextItemSeparator);
            for (int i = 0; i < parts.Length; i++)
            {
                result.Add(Uri.UnescapeDataString(parts[i]));
            }
            return result;
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
