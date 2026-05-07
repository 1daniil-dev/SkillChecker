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
                ReviewItem item = new ReviewItem();
                item.Number = (i + 1).ToString();
                item.QuestionText = _questions[i].Text;
                item.IsAnswered = _selectedAnswers[i].Count > 0;
                item.Index = i;
                item.ReviewAccessibilityName = "Вопрос " + (i + 1) + ": " + _questions[i].Text + (_selectedAnswers[i].Count > 0 ? ", отвечен" : ", не отвечен");
                items.Add(item);

                if (_selectedAnswers[i].Count > 0)
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
