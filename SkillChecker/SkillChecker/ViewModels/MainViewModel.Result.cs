using SkillChecker.Common.Models;

namespace SkillChecker.ViewModels
{
    public partial class MainViewModel
    {
        private void ShowResult()
        {
            SaveCurrentAnswer();
            _testTimer.Stop();
            ProgressValue = 100;

            try
            {
                TestResult result = _clientService.SubmitAnswers(StudentName, StudentGroup, _selectedTestName, _selectedAnswers, _textAnswers);

                ResultScore = result.Score + "%";
                ResultCorrect = "Правильных ответов: " + result.CorrectAnswers + " из " + result.TotalQuestions;

                IsPerfectScore = result.Score >= 100.0;

                if (result.Score >= 100.0) ScoreLevel = 5;
                else if (result.Score >= 90.0) ScoreLevel = 4;
                else if (result.Score >= 75.0) ScoreLevel = 3;
                else if (result.Score >= 50.0) ScoreLevel = 2;
                else if (result.Score >= 25.0) ScoreLevel = 1;
                else ScoreLevel = 0;

                if (_isPerfectScore)
                {
                    ResultScore = "100%";
                }

                List<ResultItem> items = new List<ResultItem>();
                for (int i = 0; i < _questions.Count; i++)
                {
                    Question q = _questions[i];
                    List<int> selectedList = i < _selectedAnswers.Count ? _selectedAnswers[i] : new List<int>();
                    int correctIdx = i < result.Answers.Count ? result.Answers[i].CorrectIndex : 0;
                    bool isCorrect = i < result.Answers.Count && result.Answers[i].IsCorrect;

                    string selectedText = "";
                    string correctText = "";

                    if (q.Type == QuestionTypes.Text)
                    {
                        string typed = i < _textAnswers.Count ? _textAnswers[i] : "";
                        selectedText = typed.Length > 0 ? typed : "Пропущен";

                        List<string> acceptable = i < result.Answers.Count ? result.Answers[i].AcceptableAnswers : new List<string>();
                        if (acceptable != null && acceptable.Count > 0)
                        {
                            for (int j = 0; j < acceptable.Count; j++)
                            {
                                if (j > 0) correctText += ", ";
                                correctText += acceptable[j];
                            }
                        }
                        else
                        {
                            correctText = "?";
                        }
                    }
                    else if (q.Type == QuestionTypes.Multiple)
                    {
                        if (selectedList.Count > 0)
                        {
                            for (int j = 0; j < selectedList.Count; j++)
                            {
                                if (j > 0) selectedText += ", ";
                                int s = selectedList[j];
                                selectedText += s >= 0 && s < q.Options.Count ? q.Options[s] : "?";
                            }
                        }
                        else
                        {
                            selectedText = "Пропущен";
                        }

                        if (q.CorrectAnswerIndices.Count > 0)
                        {
                            for (int j = 0; j < q.CorrectAnswerIndices.Count; j++)
                            {
                                if (j > 0) correctText += ", ";
                                int c = q.CorrectAnswerIndices[j];
                                correctText += c < q.Options.Count ? q.Options[c] : "?";
                            }
                        }
                        else
                        {
                            correctText = correctIdx < q.Options.Count ? q.Options[correctIdx] : "?";
                        }
                    }
                    else
                    {
                        int selected = selectedList.Count > 0 ? selectedList[0] : -1;
                        selectedText = selected >= 0 && selected < q.Options.Count ? q.Options[selected] : "Пропущен";
                        correctText = correctIdx < q.Options.Count ? q.Options[correctIdx] : "?";
                    }

                    ResultItem item = new ResultItem();
                    item.Number = (i + 1).ToString();
                    item.QuestionText = q.Text;
                    item.SelectedAnswer = selectedText;
                    item.CorrectAnswer = correctText;
                    item.IsCorrect = isCorrect;
                    items.Add(item);
                }

                ResultItems = items;
                AppState = AppState.Result;
            }
            catch (Exception ex)
            {
                ResultScore = "Ошибка";
                ResultCorrect = ex.Message;
                ResultItems = new List<ResultItem>();
                AppState = AppState.Result;
            }
        }
    }
}
