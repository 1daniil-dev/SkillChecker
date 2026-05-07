using System.Windows;
using System.Windows.Threading;
using SkillChecker.Common.Models;
using SkillChecker.Services;

namespace SkillChecker.ViewModels
{
    public partial class MainViewModel
    {
        private void ShowWaitScreen(string testName, DateTime scheduledTime, int timeMinutes)
        {
            WaitTestName = testName;
            WaitTimeText = "Тест начнётся в " + scheduledTime.ToString("HH:mm");
            if (timeMinutes > 0)
            {
                WaitTimeText += " (лимит: " + timeMinutes + " мин)";
            }
            UpdateWaitCountdown(scheduledTime);
            _waitTimer.Start();
            AppState = AppState.Wait;
        }

        private void WaitTimerTick(object? sender, EventArgs e)
        {
            ScheduledTest? scheduled = null;
            for (int i = 0; i < _scheduledTests.Count; i++)
            {
                if (_scheduledTests[i].Name == _waitTestName)
                {
                    scheduled = _scheduledTests[i];
                    break;
                }
            }

            if (scheduled != null)
            {
                UpdateWaitCountdown(scheduled.ScheduledTime);
            }
        }

        private void UpdateWaitCountdown(DateTime scheduledTime)
        {
            TimeSpan remaining = scheduledTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                WaitCountdownText = "00:00:00";
                CanStartFromWait = true;
                _waitTimer.Stop();
            }
            else
            {
                int hours = (int)remaining.TotalHours;
                int minutes = remaining.Minutes;
                int seconds = remaining.Seconds;
                WaitCountdownText = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
                CanStartFromWait = false;
            }
        }

        private void UpdateWaitButtonHelpText()
        {
            if (_canStartFromWait)
            {
                WaitButtonHelpText = "Время наступило, нажмите чтобы начать тест";
            }
            else
            {
                WaitButtonHelpText = "Кнопка станет активной когда наступит время начала теста";
            }
        }

        private bool CheckCanStartFromWait(object? parameter)
        {
            return _canStartFromWait;
        }

        private void ExecuteStartFromWait(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(StudentName) || string.IsNullOrWhiteSpace(StudentGroup))
            {
                StatusMessage = "Введите ФИО и группу перед началом теста";
                _waitTimer.Stop();
                AppState = AppState.Auth;
                return;
            }

            _waitTimer.Stop();
            StartTestByName(_waitTestName);
        }
    }
}
