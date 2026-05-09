namespace SkillChecker.Common.Models
{
    public static class AnswerChecker
    {
        public static bool CheckAnswer(List<int> selected, List<int> correct, string questionType)
        {
            if (questionType == "Multiple")
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
            if (typedAnswer == null || typedAnswer.Length == 0)
            {
                return false;
            }
            if (acceptableAnswers == null || acceptableAnswers.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < acceptableAnswers.Count; i++)
            {
                if (string.Equals(typedAnswer, acceptableAnswers[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
