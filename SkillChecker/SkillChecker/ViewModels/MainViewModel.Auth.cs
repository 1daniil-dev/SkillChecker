using System.Windows;
using SkillChecker.Common.Models;
using SkillChecker.Models;
using SkillChecker.Services;

namespace SkillChecker.ViewModels
{
    public partial class MainViewModel
    {
        private void ExecuteConnect(object? parameter)
        {
            try
            {
                if (!ValidateIpAndPort())
                    return;

                _clientService.ServerIp = _serverIp;
                if (int.TryParse(_serverPort, out int port))
                {
                    _clientService.ServerPort = port;
                }

                List<string> tests = _clientService.GetTestList();
                TestNames = tests;

                List<ScheduledTest> scheduled = _clientService.GetScheduledTests();
                ScheduledTests = scheduled;

                BuildTestCards(tests, scheduled);

                if (tests.Count > 0)
                {
                    TestCountText = "Доступно тестов: " + tests.Count;
                }
                else
                {
                    TestCountText = "Тестов нет";
                }

                if (scheduled.Count > 0)
                {
                    TestCountText += " | Запланировано: " + scheduled.Count;
                }

                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                TestCountText = "";
                StatusMessage = "Ошибка подключения: " + ex.Message;
            }
        }

        private bool ValidateIpAndPort()
        {
            string ip = _serverIp.Trim();
            if (ip.Length == 0)
            {
                MessageBox.Show("Введите IP-адрес сервера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string[] octets = ip.Split('.');
            if (octets.Length != 4)
            {
                MessageBox.Show("Неверный формат IP-адреса (например, 127.0.0.1)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            for (int i = 0; i < octets.Length; i++)
            {
                if (!int.TryParse(octets[i], out int octet) || octet < 0 || octet > 255)
                {
                    MessageBox.Show("Неверный IP-адрес: каждое число от 0 до 255", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (!int.TryParse(_serverPort, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Неверный порт (должен быть от 1 до 65535)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool CanRefresh(object? parameter)
        {
            return _isConnected;
        }

        private void ExecuteRefresh(object? parameter)
        {
            try
            {
                List<string> tests = _clientService.GetTestList();
                TestNames = tests;

                List<ScheduledTest> scheduled = _clientService.GetScheduledTests();
                ScheduledTests = scheduled;

                BuildTestCards(tests, scheduled);

                if (tests.Count > 0)
                {
                    TestCountText = "Доступно тестов: " + tests.Count;
                }
                else
                {
                    TestCountText = "Тестов нет";
                }

                if (scheduled.Count > 0)
                {
                    TestCountText += " | Запланировано: " + scheduled.Count;
                }

                StatusMessage = "Данные обновлены";
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка обновления: " + ex.Message;
            }
        }

        private void BuildTestCards(List<string> tests, List<ScheduledTest> scheduled)
        {
            List<TestCardItem> cards = new List<TestCardItem>();

            for (int i = 0; i < tests.Count; i++)
            {
                TestCardItem card = new TestCardItem();
                card.Name = tests[i];
                card.IsScheduled = false;
                card.ScheduleTime = "";
                card.TimeMinutes = 0;

                for (int j = 0; j < scheduled.Count; j++)
                {
                    if (scheduled[j].Name == tests[i])
                    {
                        card.TimeMinutes = scheduled[j].TimeMinutes;
                        if (scheduled[j].ScheduledTime > DateTime.Now)
                        {
                            card.IsScheduled = true;
                            card.ScheduleTime = scheduled[j].ScheduledTime.ToString("HH:mm");
                        }
                        break;
                    }
                }

                cards.Add(card);
            }

            TestCards = cards;
        }

        private bool CanStartTest(object? parameter)
        {
            return TestNames.Count > 0
                && !string.IsNullOrWhiteSpace(StudentName)
                && !string.IsNullOrWhiteSpace(StudentGroup)
                && !string.IsNullOrWhiteSpace(SelectedTestName);
        }

        private void ExecuteStartTest(object? parameter)
        {
            StartTestByName(_selectedTestName);
        }

        private void ExecuteSelectTestCard(object? parameter)
        {
            if (parameter != null)
            {
                string testName = parameter.ToString() ?? "";
                if (testName.Length > 0)
                {
                    if (string.IsNullOrWhiteSpace(StudentName) || string.IsNullOrWhiteSpace(StudentGroup))
                    {
                        IsNameError = string.IsNullOrWhiteSpace(StudentName);
                        IsGroupError = string.IsNullOrWhiteSpace(StudentGroup);
                        StatusMessage = "Введите ФИО и группу перед началом теста";
                        return;
                    }

                    IsNameError = false;
                    IsGroupError = false;

                    SelectedTestName = testName;

                    bool isScheduled = false;
                    for (int i = 0; i < _testCards.Count; i++)
                    {
                        if (_testCards[i].Name == testName && _testCards[i].IsScheduled)
                        {
                            isScheduled = true;
                            break;
                        }
                    }

                    if (isScheduled)
                    {
                        ScheduledTest? sched = null;
                        for (int i = 0; i < _scheduledTests.Count; i++)
                        {
                            if (_scheduledTests[i].Name == testName)
                            {
                                sched = _scheduledTests[i];
                                break;
                            }
                        }

                        if (sched != null && sched.ScheduledTime > DateTime.Now)
                        {
                            ShowWaitScreen(testName, sched.ScheduledTime, sched.TimeMinutes);
                            return;
                        }
                    }

                    StartTestByName(testName);
                }
            }
        }

        private void ExecuteRestart(object? parameter)
        {
            _waitTimer.Stop();
            _testTimer.Stop();
            _selectedAnswers = new List<List<int>>();
            _currentQuestionIndex = 0;
            SelectedOptionIndex = -1;
            CurrentMultipleSelected = new List<int>();
            SelectedTestName = "";
            ProgressValue = 0;
            TimerVisibility = Visibility.Collapsed;
            TimerText = "";
            IsConnected = false;
            TestCountText = "";
            StatusMessage = "Введите IP сервера и подключитесь";
            AppState = AppState.Auth;
        }

        private void ExecuteExit(object? parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
