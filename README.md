<h1 align="center">SkillChecker</h1>

<p align="center">
  <img src="https://img.shields.io/github/repo-size/1daniil-dev/SkillChecker" alt="Repo size">
  <img src="https://img.shields.io/github/languages/top/1daniil-dev/SkillChecker" alt="Top language">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/WPF-UI-0288D1" alt="WPF">
  <img src="https://img.shields.io/badge/ASP.NET-8-512BD4?logo=dotnet" alt="ASP.NET">
  <img src="https://img.shields.io/badge/SQLite-EF_Core-003B57?logo=sqlite" alt="SQLite">
  <img src="https://img.shields.io/badge/xUnit-тесты-2EA44F" alt="xUnit">
</p>

Клиент-серверная система для проведения тестов. Преподаватель запускает приложение-лаунчер — в одном окне поднимаются сервер и веб-панель; студенты проходят тестирование через WPF-клиент и получают результат.

## Возможности

- Приложение преподавателя: сервер и веб-панель в один клик, IP и порт с копированием
- Создание, редактирование, загрузка и удаление тестов через веб-панель
- Предпросмотр теста перед запуском
- Планирование времени начала теста
- Лимит времени на тест с автосдачей
- Обратный отсчёт до начала запланированного теста
- Навигация по вопросам, пропуск, проверка перед отправкой
- Подробный результат с правильными/неправильными ответами
- Три типа вопросов: одиночный выбор, множественный выбор, текстовый ввод
- Нормализация текстовых ответов (триминг, регистр, сжатие пробелов)
- Поиск по ФИО, группе и тесту на вкладке результатов
- Сортировка результатов по дате и баллу
- Экспорт результатов в Excel с фильтрацией по группам
- Защита веб-панели паролем (PBKDF2, 210 000 итераций)
- Доступность интерфейса для средств автоматизации (UI Automation)
- Логирование ошибок в консоль сервера

## Технологии

| Технология | Где используется |
|------------|------------------|
| C# .NET 8 | Все проекты |
| WPF (XAML) | Клиент (SkillChecker), лаунчер (SkillChecker.Teacher) |
| TcpListener / TcpClient | Сервер-клиент (обмен данными) |
| ASP.NET Core Minimal API | Веб-панель (SkillChecker.Web) |
| Entity Framework Core + SQLite | Хранение результатов (SkillChecker.Data) |
| ClosedXML | Экспорт в Excel |
| System.Text.Json | Сериализация тестов и результатов |
| xUnit | Модульные тесты (SkillChecker.Tests) |

## Структура проекта

```
SkillChecker/
├── SkillChecker.Common/            общая библиотека
│   ├── Models/                     Question, QuestionView, TestResult, StudentAnswer, AnswerChecker
│   ├── Protocol/                   Commands, ProtocolHelper, ProtocolFramer (length-prefixed framing)
│   └── Security/                   PasswordHasher (PBKDF2, 210 000 итераций)
├── SkillCheckerServer/             TCP-сервер (консоль)
│   ├── Program.cs                  запуск, интерактивное меню (1-6, ?)
│   ├── Server.cs                   приём подключений, многопоточность, логирование
│   ├── Server.Commands.cs          обработка команд (GET_TESTS, GET_TEST, SUBMIT, CHECK_START, GET_TEST_SETTINGS)
│   ├── Server.Results.cs           подсчёт результатов, сохранение в JSON и SQLite, цветная таблица
│   ├── Server.Settings.cs          загрузка JSON-тестов, расписание, настройки видимости
│   └── Tests/                      JSON-файлы тестов и test_settings.json
├── SkillChecker/                   WPF-клиент (студенты), MVVM
│   ├── ViewModels/                 MainViewModel (Auth, Wait, Testing, Review, Result)
│   ├── Services/                   ClientService (TCP-клиент)
│   ├── Commands/                   RelayCommand (реализация ICommand)
│   ├── Models/                     OptionItem, ResultItem, ReviewItem, TestCardItem
│   └── MainWindow.xaml             5 экранов, Unicode-иконки, глобальные хоткеи
├── SkillChecker.Teacher/           приложение преподавателя (WPF-лаунчер)
│   └── MainWindow.xaml(.cs)        окно: статус, IP и порт с копированием, открытие панели
├── SkillChecker.Web/               веб-панель преподавателя (ASP.NET Core Minimal API)
│   ├── WebPanelHost.cs             сборка приложения (запуск отдельно или из лаунчера)
│   ├── Endpoints/                  AuthEndpoints, TestsEndpoints, ResultsEndpoints, SettingsEndpoints
│   ├── Services/                   ExcelExportService (ClosedXML)
│   ├── Models/                     ErrorResult, OperationResult, ResultListItem, SettingsListItem, TestListItem
│   └── wwwroot/                    index.html (2 вкладки), editor, login, setup, JS, CSS
├── SkillChecker.Data/              хранение данных
│   ├── AppDbContext.cs             контекст EF Core (SQLite, провайдер Microsoft.Data.Sqlite)
│   └── ResultEntity.cs             сущность таблицы Results
└── SkillChecker.Tests/             модульные тесты (xUnit, 69 тестов)
    ├── CheckAnswerTests.cs         проверка Single/Multiple
    ├── CheckTextAnswerTests.cs     проверка Text с нормализацией
    ├── NormalizeTextTests.cs       функция нормализации текста
    ├── ProtocolFramerTests.cs      length-prefixed фрейминг
    └── ProtocolHelperTests.cs      сборка/разбор команд протокола
```

## Архитектура

```
студент (WPF-клиент) ────TCP:9000────┐
                                     ├── лаунчер преподавателя (SkillChecker.Teacher)
преподаватель (браузер) ──HTTP:5000──┘   внутри: TCP-сервер + веб-панель + SQLite
```

Лаунчер запускает сервер и веб-панель в одном процессе. Сервер и панель можно запускать и по отдельности — лаунчер является удобной обёрткой над ними.

## Запуск

Приложение преподавателя (рекомендуется):

1. Запустить `SkillChecker.Teacher.exe` — сервер (порт 9000) и веб-панель (порт 5000) поднимутся автоматически
2. Панель откроется в браузере; IP и порт для студентов показаны в окне (кнопка «Копировать»)
3. При первом запуске панель попросит придумать пароль преподавателя

В Visual Studio:

1. Открыть SkillChecker.slnx
2. Свойства решения → Несколько запускаемых проектов → Start для SkillChecker и SkillChecker.Teacher
3. F5 — запускаются клиент студента и лаунчер

Без Visual Studio (раздельный запуск):

```powershell
# сервер
dotnet SkillCheckerServer.dll
# клиент
SkillChecker.exe
# веб-панель
dotnet Web.dll
```

## Тестирование

```powershell
dotnet test
```

Проект `SkillChecker.Tests` содержит 69 xUnit-тестов (5 классов):

| Тестовый класс | Что проверяет |
|----------------|---------------|
| `CheckAnswerTests` | Сравнение ответов Single и Multiple |
| `CheckTextAnswerTests` | Сравнение текстовых ответов с нормализацией |
| `NormalizeTextTests` | Функция нормализации текста |
| `ProtocolFramerTests` | Length-prefixed фрейминг (кодирование/декодирование) |
| `ProtocolHelperTests` | Сборка и разбор команд протокола |
