using SkillChecker.Common.Models;

namespace SkillChecker.ViewModels
{
    public partial class MainViewModel
    {
        private void ShowReview()
        {
            List<ReviewItem> items = new List<ReviewItem>();
            int answeredCount = 0;

            for (int i = 0; i < _questions.Count; i++)
            {
                bool isAnswered;
                if (_questions[i].Type == QuestionTypes.Text)
                {
                    string saved = i < _textAnswers.Count ? _textAnswers[i] : "";
                    isAnswered = saved != null && saved.Length > 0;
                }
                else
                {
                    isAnswered = _selectedAnswers[i].Count > 0;
                }

                ReviewItem item = new ReviewItem();
                item.Number = (i + 1).ToString();
                item.QuestionText = _questions[i].Text;
                item.IsAnswered = isAnswered;
                item.Index = i;
                item.ReviewAccessibilityName = "Вопрос " + (i + 1) + ": " + _questions[i].Text + (isAnswered ? ", отвечен" : ", не отвечен");
                items.Add(item);

                if (isAnswered)
                {
                    answeredCount++;
                }
            }

            ReviewItems = items;
            AppState = AppState.Review;
        }

        private void ExecuteSubmit(object? parameter)
        {
            ShowResult();
        }
    }
}
