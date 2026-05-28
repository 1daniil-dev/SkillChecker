<h1 align="center">SkillChecker</h1>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/WPF-UI-0288D1" alt="WPF">
  <img src="https://img.shields.io/badge/ASP.NET-8-512BD4?logo=dotnet" alt="ASP.NET">
  <img src="https://img.shields.io/badge/SQLite-EF_Core-003B57?logo=sqlite" alt="SQLite">
  <img src="https://img.shields.io/badge/xUnit-тесты-2EA44F" alt="xUnit">
</p>

Клиент-серверная система для проведения тестов. Преподаватель загружает тесты и управляет ими через веб-панель, студенты проходят тестирование через WPF-клиент и получают результат.

## Возможности

- Загрузка и удаление тестов через веб-панель
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
- Защита веб-панели паролем (SHA-256)
- Доступность интерфейса для средств автоматизации (UI Automation)
- Логирование ошибок в консоль сервера

## Технологии

| Технология | Где используется |
|------------|------------------|
| C# .NET 8 | Все проекты |
| WPF (XAML) | Клиент (SkillChecker) |
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
│   └── Security/                   PasswordHasher (SHA-256)
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
│   └── MainWindow.xaml             5 экранов, Segoe MDL2 иконки, глобальные хоткеи
├── SkillChecker.Web/               веб-панель преподавателя (ASP.NET Core Minimal API)
│   ├── Program.cs                  настройка, middleware, авторизация, вызов Endpoints
│   ├── Endpoints/                  AuthEndpoints, TestsEndpoints, ResultsEndpoints, SettingsEndpoints
│   ├── Services/                   ExcelExportService (ClosedXML)
│   ├── Models/                     ErrorResult, OperationResult, ResultListItem, SettingsListItem, TestListItem
│   └── wwwroot/                    index.html (2 вкладки), JS (4 модуля), CSS
├── SkillChecker.Data/              хранение данных
│   ├── AppDbContext.cs             контекст EF Core (SQLite, провайдер Microsoft.Data.Sqlite)
│   └── ResultEntity.cs             сущность таблицы Results
└── SkillChecker.Tests/             модульные тесты (xUnit)
    ├── CheckAnswerTests.cs         проверка Single/Multiple
    ├── CheckTextAnswerTests.cs     проверка Text с нормализацией
    ├── NormalizeTextTests.cs       функция нормализации текста
    ├── ProtocolFramerTests.cs      length-prefixed фрейминг
    └── ProtocolHelperTests.cs      сборка/разбор команд протокола
```

## Архитектура

```
студент (WPF) ──TCP:9000──> сервер (консоль) <──HTTP:5000── преподаватель (веб)
                                    │
                                 SQLite
```

## Запуск

1. Открыть SkillChecker.slnx в Visual Studio
2. Свойства решения → Несколько запускаемых проектов → Start для SkillCheckerServer, SkillChecker, SkillChecker.Web
3. F5 — запускаются сервер, клиент и веб-панель
4. В клиенте: IP 127.0.0.1, порт 9000, подключиться, выбрать тест, пройти
5. Веб-панель: http://localhost:5000 (при первом запуске потребуется придумать пароль преподавателя)

Без Visual Studio:

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

Проект `SkillChecker.Tests` содержит xUnit-тесты (5 классов):

| Тестовый класс | Что проверяет |
|----------------|---------------|
| `CheckAnswerTests` | Сравнение ответов Single и Multiple |
| `CheckTextAnswerTests` | Сравнение текстовых ответов с нормализацией |
| `NormalizeTextTests` | Функция нормализации текста |
| `ProtocolFramerTests` | Length-prefixed фрейминг (кодирование/декодирование) |
| `ProtocolHelperTests` | Сборка и разбор команд протокола |
