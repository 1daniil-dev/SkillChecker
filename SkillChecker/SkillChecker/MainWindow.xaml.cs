using System.Windows;
using System.Windows.Input;
using SkillChecker.ViewModels;

namespace SkillChecker
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel.AppState == AppState.Testing)
            {
                if (e.Key == Key.Enter)
                {
                    _viewModel.NextQuestionCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    _viewModel.CancelTestCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                {
                    _viewModel.ToggleOptionCommand.Execute(0);
                    e.Handled = true;
                }
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    _viewModel.ToggleOptionCommand.Execute(1);
                    e.Handled = true;
                }
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    _viewModel.ToggleOptionCommand.Execute(2);
                    e.Handled = true;
                }
                else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                {
                    _viewModel.ToggleOptionCommand.Execute(3);
                    e.Handled = true;
                }
                else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
                {
                    _viewModel.ToggleOptionCommand.Execute(4);
                    e.Handled = true;
                }
                else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
                {
                    _viewModel.ToggleOptionCommand.Execute(5);
                    e.Handled = true;
                }
            }
            else if (_viewModel.AppState == AppState.Review)
            {
                if (e.Key == Key.Enter)
                {
                    _viewModel.SubmitCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    _viewModel.CancelTestCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (_viewModel.AppState == AppState.Wait)
            {
                if (e.Key == Key.Enter && _viewModel.CanStartFromWait)
                {
                    _viewModel.StartFromWaitCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (_viewModel.AppState == AppState.Result)
            {
                if (e.Key == Key.Enter)
                {
                    _viewModel.RestartCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void ShowHotkeys_Click(object sender, RoutedEventArgs e)
        {
            string text = "Горячие клавиши:\n\n" +
                "Тестирование:\n" +
                "  Enter — следующий вопрос\n" +
                "  Esc — выйти из теста\n" +
                "  1-6 — выбрать вариант ответа\n\n" +
                "Проверка ответов:\n" +
                "  Enter — отправить результат\n" +
                "  Esc — выйти из теста\n\n" +
                "Ожидание теста:\n" +
                "  Enter — начать тест\n\n" +
                "Результат:\n" +
                "  Enter — начать заново";
            MessageBox.Show(text, "Горячие клавиши", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            string text = "SkillChecker — Система тестирования учащихся\n\n" +
                "Версия 1.0\n\n" +
                "Клиент-серверная система для проведения тестов.\n" +
                "Преподаватель запускает сервер, студенты\n" +
                "подключаются через клиент и проходят тестирование.";
            MessageBox.Show(text, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
