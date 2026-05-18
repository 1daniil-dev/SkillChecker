using System.Text;

namespace SkillChecker.Common.Models
{
    public static class AnswerChecker
    {
        public static bool CheckAnswer(List<int> selected, List<int> correct, string questionType)
        {
            if (questionType == QuestionTypes.Multiple)
            {
                if (selected.Count != correct.Count)
                {
                    return false;
                }
                for (int i = 0; i < selected.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < correct.Count; j++)
                    {
                        if (selected[i] == correct[j])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                if (selected.Count > 0)
                {
                    return selected[0] == correct[0];
                }
                return false;
            }
        }

        public static bool CheckTextAnswer(string typedAnswer, List<string> acceptableAnswers)
        {
            string normalizedTyped = NormalizeText(typedAnswer);
            if (normalizedTyped.Length == 0)
            {
                return false;
            }
            if (acceptableAnswers == null || acceptableAnswers.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < acceptableAnswers.Count; i++)
            {
                string normalizedAcceptable = NormalizeText(acceptableAnswers[i]);
                if (string.Equals(normalizedTyped, normalizedAcceptable, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public static string NormalizeText(string text)
        {
            if (text == null || text.Length == 0)
            {
                return "";
            }
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                return "";
            }
            string lowered = trimmed.ToLowerInvariant();
            StringBuilder builder = new StringBuilder(lowered.Length);
            bool prevSpace = false;
            for (int i = 0; i < lowered.Length; i++)
            {
                char current = lowered[i];
                if (current == 'ё')
                {
                    current = 'е';
                }
                bool isSpace = current == ' ' || current == '\t' || current == '\n' || current == '\r';
                if (isSpace)
                {
                    if (prevSpace)
                    {
                        continue;
                    }
                    builder.Append(' ');
                    prevSpace = true;
                }
                else
                {
                    builder.Append(current);
                    prevSpace = false;
                }
            }
            return builder.ToString();
        }
    }
}
