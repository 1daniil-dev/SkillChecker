using System.Windows;
using SkillChecker.Common.Models;
using SkillChecker.Models;
using SkillChecker.Services;

namespace SkillChecker.ViewModels
{
    public partial class MainViewModel
    {
        private void StartTestByName(string testName)
        {
            try
            {
                TestQuestionsResult testResult = _clientService.GetTestQuestions(testName);

                if (testResult.IsWaiting)
                {
                    ShowWaitScreen(testName, testResult.WaitTime, testResult.TimeMinutes);
                    return;
                }

                if (testResult.Questions.Count == 0)
                {
                    StatusMessage = "Тест пуст";
                    return;
                }

                _questions = testResult.Questions;
                _selectedAnswers = new List<List<int>>();
                _textAnswers = new List<string>();
                for (int i = 0; i < _questions.Count; i++)
                {
                    _selectedAnswers.Add(new List<int>());
                    _textAnswers.Add("");
                }
                _currentQuestionIndex = 0;
                TotalQuestions = _questions.Count;
                ShowQuestion(0);
                AppState = AppState.Testing;

                _testTimeMinutes = testResult.TimeMinutes;
                if (_testTimeMinutes > 0)
                {
                    _testEndTime = DateTime.Now.AddMinutes(_testTimeMinutes);
                    TimerVisibility = Visibility.Visible;
                    UpdateTestTimer();
                    _testTimer.Start();
                }
                else
                {
                    TimerVisibility = Visibility.Collapsed;
                    TimerText = "";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void TestTimerTick(object? sender, EventArgs e)
        {
            UpdateTestTimer();
        }

        private void UpdateTestTimer()
        {
            TimeSpan remaining = _testEndTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                TimerText = "00:00";
                _testTimer.Stop();
                AutoSubmit();
                return;
            }

            int totalMinutes = (int)remaining.TotalMinutes;
            int seconds = remaining.Seconds;
            TimerText = totalMinutes.ToString("D2") + ":" + seconds.ToString("D2");
        }

        private void AutoSubmit()
        {
            SaveCurrentAnswer();
            MessageBox.Show("Время вышло! Ответы отправлены.", "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowResult();
        }

        private void SaveCurrentAnswer()
        {
            if (_currentQuestionIndex >= 0 && _currentQuestionIndex < _selectedAnswers.Count)
            {
                if (_isCurrentText)
                {
                    string trimmed = _currentTextAnswer == null ? "" : _currentTextAnswer.Trim();
                    _textAnswers[_currentQuestionIndex] = trimmed;
                    _selectedAnswers[_currentQuestionIndex] = new List<int>();
                }
                else if (_isCurrentMultiple)
                {
                    _selectedAnswers[_currentQuestionIndex] = new List<int>(_currentMultipleSelected);
                    _textAnswers[_currentQuestionIndex] = "";
                }
                else
                {
                    List<int> single = new List<int>();
                    if (_selectedOptionIndex >= 0)
                    {
                        single.Add(_selectedOptionIndex);
                    }
                    _selectedAnswers[_currentQuestionIndex] = single;
                    _textAnswers[_currentQuestionIndex] = "";
                }
            }
        }

        private bool CanNextQuestion(object? parameter)
        {
            return true;
        }

        private void ExecuteNextQuestion(object? parameter)
        {
            SaveCurrentAnswer();

            if (_currentQuestionIndex < _questions.Count - 1)
            {
                ShowQuestion(_currentQuestionIndex + 1);
            }
            else
            {
                ShowReview();
            }
        }

        private bool CanPrevQuestion(object? parameter)
        {
            return CanGoBack;
        }

        private void ExecutePrevQuestion(object? parameter)
        {
            SaveCurrentAnswer();
            ShowQuestion(_currentQuestionIndex - 1);
        }

        private void ExecuteGoToQuestion(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int index))
            {
                if (index >= 0 && index < _questions.Count)
                {
                    SaveCurrentAnswer();
                    ShowQuestion(index);
                    AppState = AppState.Testing;
                }
            }
        }

        private void ExecuteToggleOption(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int optionIndex))
            {
                if (_isCurrentMultiple)
                {
                    int existingIndex = -1;
                    for (int i = 0; i < _currentMultipleSelected.Count; i++)
                    {
                        if (_currentMultipleSelected[i] == optionIndex)
                        {
                            existingIndex = i;
                            break;
                        }
                    }

                    if (existingIndex >= 0)
                    {
                        _currentMultipleSelected.RemoveAt(existingIndex);
                    }
                    else
                    {
                        _currentMultipleSelected.Add(optionIndex);
                    }

                    List<int> updated = new List<int>(_currentMultipleSelected);
                    CurrentMultipleSelected = updated;

                    for (int i = 0; i < _currentOptions.Count; i++)
                    {
                        _currentOptions[i].IsSelected = false;
                        for (int j = 0; j < updated.Count; j++)
                        {
                            if (updated[j] == _currentOptions[i].Index)
                            {
                                _currentOptions[i].IsSelected = true;
                                break;
                            }
                        }
                    }
                    List<OptionItem> updatedOptions = new List<OptionItem>(_currentOptions);
                    CurrentOptions = updatedOptions;
                }
                else
                {
                    SelectedOptionIndex = optionIndex;

                    for (int i = 0; i < _currentOptions.Count; i++)
                    {
                        _currentOptions[i].IsSelected = _currentOptions[i].Index == optionIndex;
                    }
                    List<OptionItem> updatedOptions = new List<OptionItem>(_currentOptions);
                    CurrentOptions = updatedOptions;
                }
            }
        }

        private void ShowQuestion(int index)
        {
            _currentQuestionIndex = index;
            Question q = _questions[index];
            QuestionText = q.Text;
            IsCurrentText = q.Type == QuestionTypes.Text;
            IsCurrentMultiple = q.Type == QuestionTypes.Multiple;
            if (_isCurrentText)
            {
                QuestionTypeHint = "Введите ответ";
            }
            else if (_isCurrentMultiple)
            {
                QuestionTypeHint = "Выберите несколько вариантов ответа";
            }
            else
            {
                QuestionTypeHint = "Выберите один вариант ответа";
            }

            List<int> savedAnswers = _selectedAnswers[index];
            string savedText = index < _textAnswers.Count ? _textAnswers[index] : "";

            if (_isCurrentText)
            {
                CurrentOptions = new List<OptionItem>();
                CurrentMultipleSelected = new List<int>();
                SelectedOptionIndex = -1;
                CurrentTextAnswer = savedText;
            }
            else
            {
                List<OptionItem> options = new List<OptionItem>();
                for (int i = 0; i < q.Options.Count; i++)
                {
                    OptionItem opt = new OptionItem();
                    opt.Index = i;
                    opt.Text = q.Options[i];
                    opt.IsSelected = false;

                    if (_isCurrentMultiple)
                    {
                        for (int j = 0; j < savedAnswers.Count; j++)
                        {
                            if (savedAnswers[j] == i)
                            {
                                opt.IsSelected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (savedAnswers.Count > 0 && savedAnswers[0] == i)
                        {
                            opt.IsSelected = true;
                        }
                    }

                    options.Add(opt);
                }
                CurrentOptions = options;

                if (_isCurrentMultiple)
                {
                    CurrentMultipleSelected = new List<int>(savedAnswers);
                    SelectedOptionIndex = -1;
                }
                else
                {
                    SelectedOptionIndex = savedAnswers.Count > 0 ? savedAnswers[0] : -1;
                    CurrentMultipleSelected = new List<int>();
                }
                CurrentTextAnswer = "";
            }

            QuestionNumber = index + 1;
            CanGoBack = index > 0;
            NextButtonText = "Далее";

            if (_questions.Count > 0)
            {
                ProgressValue = (double)(index + 1) / _questions.Count * 100;
            }
        }

        private void ExecuteCancelTest(object? parameter)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены? Ответы не будут сохранены.",
                "Выход из теста",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _testTimer.Stop();
                _selectedAnswers = new List<List<int>>();
                _textAnswers = new List<string>();
                _currentQuestionIndex = 0;
                SelectedOptionIndex = -1;
                CurrentMultipleSelected = new List<int>();
                CurrentTextAnswer = "";
                SelectedTestName = "";
                ProgressValue = 0;
                TimerVisibility = Visibility.Collapsed;
                TimerText = "";
                AppState = AppState.Auth;
            }
        }
    }
}
