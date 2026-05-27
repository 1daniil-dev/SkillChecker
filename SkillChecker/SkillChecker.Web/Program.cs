using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SkillChecker.Common.Models;
using SkillChecker.Common.Security;
using SkillChecker.Data;
using SkillChecker.Web.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});
var app = builder.Build();

app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/index.html"));

string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
string testsFolder = Path.Combine(solutionDir, "SkillCheckerServer", "Tests");
string resultsFolder = Path.Combine(solutionDir, "SkillCheckerServer", "Results");
string settingsFile = Path.Combine(testsFolder, "test_settings.json");
string authFile = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "auth.json");
string dbPath = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "skillchecker.db");

if (!Directory.Exists(testsFolder))
{
    testsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests");
    resultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
    settingsFile = Path.Combine(testsFolder, "test_settings.json");
    authFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "auth.json");
    dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "skillchecker.db");
}

void InitializeDatabase()
{
    string? dbDir = Path.GetDirectoryName(dbPath);
    if (dbDir != null) Directory.CreateDirectory(dbDir);
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        db.Database.EnsureCreated();
    }
}

void SyncResultsToDb()
{
    if (!Directory.Exists(resultsFolder)) return;
    string[] files = Directory.GetFiles(resultsFolder, "*.json");
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            bool exists = db.Results.Any(r => r.SourceFile == fileName);
            if (exists) continue;

            try
            {
                string json = File.ReadAllText(files[i], Encoding.UTF8);
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                TestResult? result = JsonSerializer.Deserialize<TestResult>(json, options);
                if (result == null) continue;

                ResultEntity entity = new ResultEntity();
                entity.StudentName = result.StudentName;
                entity.Group = result.Group;
                entity.TestName = result.TestName;
                entity.Date = result.Date;
                entity.TotalQuestions = result.TotalQuestions;
                entity.CorrectAnswers = result.CorrectAnswers;
                entity.Score = result.Score;
                entity.AnswersJson = JsonSerializer.Serialize(result.Answers,
                    new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                entity.SourceFile = fileName;
                db.Results.Add(entity);
            }
            catch (Exception ex) { Console.WriteLine($"SyncResultsToDb error: {ex.Message}"); }
        }
        db.SaveChanges();
    }
}

InitializeDatabase();
SyncResultsToDb();

AuthData? LoadAuthData()
{
    if (!File.Exists(authFile)) return null;
    try
    {
        string json = File.ReadAllText(authFile, Encoding.UTF8);
        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<AuthData>(json, options);
    }
    catch
    {
        return null;
    }
}

bool IsAuthSetup()
{
    AuthData? data = LoadAuthData();
    return data != null && data.PasswordHash.Length > 0;
}

bool VerifyPassword(string password, out bool needsMigration)
{
    needsMigration = false;
    AuthData? data = LoadAuthData();
    if (data == null || data.PasswordHash.Length == 0) return false;
    return PasswordHasher.Verify(password, data.PasswordHash, out needsMigration);
}

void SaveAuth(string passwordHash)
{
    string? dir = Path.GetDirectoryName(authFile);
    if (dir != null) Directory.CreateDirectory(dir);
    AuthData data = new AuthData();
    data.PasswordHash = passwordHash;
    JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(authFile, JsonSerializer.Serialize(data, jsonOptions), Encoding.UTF8);
}

bool IsAuthorized(HttpContext context)
{
    string? password = context.Request.Headers["X-Auth-Password"];
    if (string.IsNullOrEmpty(password)) return false;
    bool ignored;
    return VerifyPassword(password, out ignored);
}

ParsedSettings ParseTestSettings(JsonElement elem)
{
    ParsedSettings settings = new ParsedSettings();
    if (elem.ValueKind == JsonValueKind.Object)
    {
        JsonElement startElem;
        if (elem.TryGetProperty("StartTime", out startElem) && startElem.ValueKind == JsonValueKind.String)
        {
            settings.StartTime = startElem.GetString() ?? "";
        }
        JsonElement timeElem;
        if (elem.TryGetProperty("TimeMinutes", out timeElem) && timeElem.ValueKind == JsonValueKind.Number)
        {
            settings.TimeMinutes = timeElem.GetInt32();
        }
        JsonElement visibleElem;
        if (elem.TryGetProperty("Visible", out visibleElem) && (visibleElem.ValueKind == JsonValueKind.True || visibleElem.ValueKind == JsonValueKind.False))
        {
            settings.Visible = visibleElem.GetBoolean();
        }
    }
    else if (elem.ValueKind == JsonValueKind.String)
    {
        settings.StartTime = elem.GetString() ?? "";
    }
    return settings;
}

Dictionary<string, JsonElement> LoadSettingsRaw()
{
    Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>();
    if (File.Exists(settingsFile))
    {
        string json = File.ReadAllText(settingsFile, Encoding.UTF8);
        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Dictionary<string, JsonElement>? loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
        if (loaded != null)
        {
            result = loaded;
        }
    }
    return result;
}

Dictionary<string, object> LoadSettingsAsEntries()
{
    Dictionary<string, object> data = new Dictionary<string, object>();
    Dictionary<string, JsonElement> raw = LoadSettingsRaw();
    foreach (KeyValuePair<string, JsonElement> kvp in raw)
    {
        ParsedSettings parsed = ParseTestSettings(kvp.Value);
        Dictionary<string, object> entry = new Dictionary<string, object>();
        entry["StartTime"] = parsed.StartTime;
        entry["TimeMinutes"] = parsed.TimeMinutes;
        entry["Visible"] = parsed.Visible;
        data[kvp.Key] = entry;
    }
    return data;
}

void SaveSettingsData(Dictionary<string, object> data)
{
    JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    string outJson = JsonSerializer.Serialize(data, jsonOptions);
    File.WriteAllText(settingsFile, outJson, Encoding.UTF8);
}

app.Use(async (HttpContext context, Func<Task> next) =>
{
    string path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/api/") && !path.StartsWith("/api/auth/"))
    {
        if (!IsAuthorized(context))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new ErrorResult { Error = "Не авторизован" });
            return;
        }
    }
    await next();
});

app.MapGet("/api/auth/state", () =>
{
    return Results.Json(new AuthState { Setup = IsAuthSetup() });
});

app.MapPost("/api/auth/setup", (AuthRequest req) =>
{
    if (IsAuthSetup())
    {
        return Results.BadRequest(new ErrorResult { Error = "Пароль уже установлен" });
    }
    if (req.Password == null || req.Password.Length < 4)
    {
        return Results.BadRequest(new ErrorResult { Error = "Пароль должен быть не менее 4 символов" });
    }
    SaveAuth(PasswordHasher.Hash(req.Password));
    return Results.Json(new OperationResult { Ok = true });
});

app.MapPost("/api/auth/login", (AuthRequest req) =>
{
    if (!IsAuthSetup())
    {
        return Results.BadRequest(new ErrorResult { Error = "Пароль ещё не установлен" });
    }
    if (req.Password == null)
    {
        return Results.Json(new OperationResult { Ok = false });
    }
    bool needsMigration;
    if (!VerifyPassword(req.Password, out needsMigration))
    {
        return Results.Json(new OperationResult { Ok = false });
    }
    if (needsMigration)
    {
        SaveAuth(PasswordHasher.Hash(req.Password));
    }
    return Results.Json(new OperationResult { Ok = true });
});

app.MapGet("/api/tests", () =>
{
    Directory.CreateDirectory(testsFolder);
    string[] files = Directory.GetFiles(testsFolder, "*.json");
    List<object> list = new List<object>();

    Dictionary<string, JsonElement> settingsData = LoadSettingsRaw();

    for (int i = 0; i < files.Length; i++)
    {
        string fileName = Path.GetFileName(files[i]);
        if (fileName == "test_settings.json" || fileName == "schedule.json") continue;

        string name = Path.GetFileNameWithoutExtension(files[i]);
        string json = File.ReadAllText(files[i], Encoding.UTF8);
        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(json, options);
        int count = questions != null ? questions.Count : 0;

        ParsedSettings parsed = new ParsedSettings();
        bool hasSettings = false;

        JsonElement settingsElem;
        if (settingsData.TryGetValue(name, out settingsElem))
        {
            parsed = ParseTestSettings(settingsElem);
            hasSettings = settingsElem.ValueKind == JsonValueKind.Object;
        }

        list.Add(new TestListItem { Name = name, QuestionCount = count, Visible = parsed.Visible, HasSettings = hasSettings, DisplayTime = parsed.DisplayTime, TimeMinutes = parsed.TimeMinutes });
    }
    return Results.Json(list);
});

app.MapGet("/api/results", () =>
{
    SyncResultsToDb();
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        List<ResultEntity> entities = db.Results.OrderByDescending(r => r.Date).ToList();
        List<object> list = new List<object>();
        JsonSerializerOptions jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        for (int i = 0; i < entities.Count; i++)
        {
            ResultEntity e = entities[i];
            List<StudentAnswer>? answers = null;
            try { answers = JsonSerializer.Deserialize<List<StudentAnswer>>(e.AnswersJson, jsonOpts); } catch (Exception ex) { Console.WriteLine($"Results deserialize error: {ex.Message}"); }
            ResultListItem item = new ResultListItem();
            item.StudentName = e.StudentName;
            item.Group = e.Group;
            item.TestName = e.TestName;
            item.Date = e.Date;
            item.TotalQuestions = e.TotalQuestions;
            item.CorrectAnswers = e.CorrectAnswers;
            item.Score = e.Score;
            item.Answers = answers ?? new List<StudentAnswer>();
            item.FileName = e.SourceFile;
            list.Add(item);
        }
        return Results.Json(list);
    }
});

app.MapDelete("/api/results/{fileName}", (string fileName) =>
{
    string safe = Path.GetFileName(fileName);
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        ResultEntity? entity = db.Results.FirstOrDefault(r => r.SourceFile == safe);
        if (entity != null)
        {
            db.Results.Remove(entity);
            db.SaveChanges();
        }
    }
    string filePath = Path.Combine(resultsFolder, safe);
    if (File.Exists(filePath)) File.Delete(filePath);
    return Results.Json(new OperationResult { Ok = true });
});

app.MapDelete("/api/results", () =>
{
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        List<ResultEntity> all = db.Results.ToList();
        for (int i = 0; i < all.Count; i++)
        {
            db.Results.Remove(all[i]);
        }
        db.SaveChanges();
    }
    if (Directory.Exists(resultsFolder))
    {
        string[] files = Directory.GetFiles(resultsFolder, "*.json");
        for (int i = 0; i < files.Length; i++)
        {
            File.Delete(files[i]);
        }
    }
    return Results.Json(new OperationResult { Ok = true });
});

app.MapPost("/api/results/export", async (HttpContext context) =>
{
    if (!IsAuthorized(context)) return Results.Unauthorized();

    using StreamReader reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();
    JsonSerializerOptions jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    ExportRequest? req = JsonSerializer.Deserialize<ExportRequest>(body, jsonOpts);
    if (req == null || req.FileNames.Count == 0)
    {
        return Results.BadRequest(new ErrorResult { Error = "Нет выбранных результатов" });
    }

    List<TestResult> results = new List<TestResult>();
    using (AppDbContext db = new AppDbContext(dbPath))
    {
        for (int i = 0; i < req.FileNames.Count; i++)
        {
            string safe = Path.GetFileName(req.FileNames[i]);
            ResultEntity? entity = db.Results.FirstOrDefault(r => r.SourceFile == safe);
            if (entity == null) continue;
            TestResult tr = new TestResult();
            tr.StudentName = entity.StudentName;
            tr.Group = entity.Group;
            tr.TestName = entity.TestName;
            tr.Date = entity.Date;
            tr.TotalQuestions = entity.TotalQuestions;
            tr.CorrectAnswers = entity.CorrectAnswers;
            tr.Score = entity.Score;
            try
            {
                List<StudentAnswer>? answers = JsonSerializer.Deserialize<List<StudentAnswer>>(entity.AnswersJson, jsonOpts);
                if (answers != null) tr.Answers = answers;
            }
            catch (Exception ex) { Console.WriteLine($"Export deserialize error: {ex.Message}"); }
            results.Add(tr);
        }
    }

    if (results.Count == 0)
    {
        return Results.BadRequest(new ErrorResult { Error = "Не найдено результатов" });
    }

    using XLWorkbook workbook = new XLWorkbook();
    IXLWorksheet ws = workbook.Worksheets.Add("Результаты");

    ws.Cell(1, 1).Value = "ФИО";
    ws.Cell(1, 2).Value = "Группа";
    ws.Cell(1, 3).Value = "Тест";
    ws.Cell(1, 4).Value = "Дата";
    ws.Cell(1, 5).Value = "Правильно";
    ws.Cell(1, 6).Value = "Всего";
    ws.Cell(1, 7).Value = "Балл %";

    IXLRange headerRange = ws.Range(1, 1, 1, 7);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4A90D9");
    headerRange.Style.Font.FontColor = XLColor.White;

    for (int i = 0; i < results.Count; i++)
    {
        int row = i + 2;
        TestResult r = results[i];
        ws.Cell(row, 1).Value = r.StudentName;
        ws.Cell(row, 2).Value = r.Group;
        ws.Cell(row, 3).Value = r.TestName;
        ws.Cell(row, 4).Value = r.Date.ToString("dd.MM.yyyy HH:mm");
        ws.Cell(row, 5).Value = r.CorrectAnswers;
        ws.Cell(row, 6).Value = r.TotalQuestions;
        ws.Cell(row, 7).Value = r.Score;

        if (r.Score >= 70)
            ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
        else if (r.Score >= 40)
            ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#F57C00");
        else
            ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#D32F2F");
    }

    ws.Columns().AdjustToContents();

    MemoryStream ms = new MemoryStream();
    workbook.SaveAs(ms);
    ms.Position = 0;
    byte[] bytes = ms.ToArray();
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "results.xlsx");
});

app.MapGet("/api/settings", () =>
{
    Dictionary<string, JsonElement> raw = LoadSettingsRaw();
    if (raw.Count == 0)
    {
        return Results.Json(new List<object>());
    }

    List<object> list = new List<object>();
    foreach (KeyValuePair<string, JsonElement> kvp in raw)
    {
        ParsedSettings parsed = ParseTestSettings(kvp.Value);
        list.Add(new SettingsListItem { TestName = kvp.Key, StartTime = parsed.StartTime, DisplayTime = parsed.DisplayTime, TimeMinutes = parsed.TimeMinutes, Visible = parsed.Visible });
    }
    return Results.Json(list);
});

app.MapPost("/api/settings", (SettingsRequest req) =>
{
    Directory.CreateDirectory(testsFolder);

    Dictionary<string, object> data = LoadSettingsAsEntries();

    if (data.ContainsKey(req.TestName))
    {
        Dictionary<string, object> existingEntry = (Dictionary<string, object>)data[req.TestName];
        Dictionary<string, object> newEntry = new Dictionary<string, object>();
        if (req.StartTime.Length > 0 && DateTime.TryParse(req.StartTime, out DateTime scheduled))
        {
            newEntry["StartTime"] = scheduled.ToString("o");
        }
        else
        {
            newEntry["StartTime"] = existingEntry.ContainsKey("StartTime") ? existingEntry["StartTime"] : "";
        }
        newEntry["TimeMinutes"] = req.TimeMinutes;
        newEntry["Visible"] = existingEntry.ContainsKey("Visible") ? existingEntry["Visible"] : true;
        data[req.TestName] = newEntry;
    }
    else
    {
        Dictionary<string, object> newEntry = new Dictionary<string, object>();
        if (req.StartTime.Length > 0 && DateTime.TryParse(req.StartTime, out DateTime scheduled))
        {
            newEntry["StartTime"] = scheduled.ToString("o");
        }
        else
        {
            newEntry["StartTime"] = "";
        }
        newEntry["TimeMinutes"] = req.TimeMinutes;
        newEntry["Visible"] = true;
        data[req.TestName] = newEntry;
    }

    SaveSettingsData(data);

    return Results.Json(new OperationResult { Ok = true });
});

app.MapDelete("/api/settings/{testName}", (string testName) =>
{
    if (!File.Exists(settingsFile))
    {
        return Results.Json(new OperationResult { Ok = true });
    }

    Dictionary<string, JsonElement> raw = LoadSettingsRaw();
    if (raw.ContainsKey(testName))
    {
        raw.Remove(testName);
        Dictionary<string, object> data = new Dictionary<string, object>();
        foreach (KeyValuePair<string, JsonElement> kvp in raw)
        {
            ParsedSettings parsed = ParseTestSettings(kvp.Value);
            Dictionary<string, object> entry = new Dictionary<string, object>();
            entry["StartTime"] = parsed.StartTime;
            entry["TimeMinutes"] = parsed.TimeMinutes;
            entry["Visible"] = parsed.Visible;
            data[kvp.Key] = entry;
        }
        SaveSettingsData(data);
    }

    return Results.Json(new OperationResult { Ok = true });
});

app.MapPatch("/api/settings/{testName}/visibility", (string testName, VisibilityRequest req) =>
{
    Directory.CreateDirectory(testsFolder);

    Dictionary<string, object> data = LoadSettingsAsEntries();

    if (data.ContainsKey(testName))
    {
        Dictionary<string, object> entry = (Dictionary<string, object>)data[testName];
        entry["Visible"] = req.Visible;
    }
    else
    {
        Dictionary<string, object> entry = new Dictionary<string, object>();
        entry["StartTime"] = "";
        entry["TimeMinutes"] = 0;
        entry["Visible"] = req.Visible;
        data[testName] = entry;
    }

    SaveSettingsData(data);

    return Results.Json(new OperationResult { Ok = true });
});

app.MapPost("/api/upload", async (HttpContext context) =>
{
    Directory.CreateDirectory(testsFolder);
    IFormCollection form = await context.Request.ReadFormAsync();
    IFormFile? file = null;
    if (form.Files.Count > 0)
    {
        file = form.Files[0];
    }
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new ErrorResult { Error = "Файл не выбран" });
    }

    string name = form["name"].ToString();
    if (name.Length == 0)
    {
        name = Path.GetFileNameWithoutExtension(file.FileName);
    }

    string filePath = Path.Combine(testsFolder, name + ".json");
    using (FileStream stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Json(new OperationResult { Ok = true, Name = name });
});

app.MapGet("/api/test/{name}/preview", (string name) =>
{
    string filePath = Path.Combine(testsFolder, name + ".json");
    if (!File.Exists(filePath))
    {
        return Results.NotFound(new ErrorResult { Error = "Тест не найден" });
    }

    string json = File.ReadAllText(filePath, Encoding.UTF8);
    JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(json, options);
    if (questions == null)
    {
        return Results.NotFound(new ErrorResult { Error = "Ошибка чтения теста" });
    }

    return Results.Json(questions);
});

app.MapDelete("/api/test/{name}", (string name) =>
{
    string filePath = Path.Combine(testsFolder, name + ".json");
    if (!File.Exists(filePath))
    {
        return Results.NotFound(new ErrorResult { Error = "Тест не найден" });
    }

    File.Delete(filePath);
    return Results.Json(new OperationResult { Ok = true });
});

app.Run();

public class SettingsRequest
{
    public string TestName { get; set; } = "";
    public string StartTime { get; set; } = "";
    public int TimeMinutes { get; set; } = 0;
}

public class VisibilityRequest
{
    public bool Visible { get; set; } = true;
}

public class AuthData
{
    private string _passwordHash;
    public string PasswordHash { get => _passwordHash; set => _passwordHash = value; }
    public AuthData()
    {
        _passwordHash = "";
    }
}

public class AuthRequest
{
    public string Password { get; set; } = "";
}

public class AuthState
{
    private bool _setup;

    [System.Text.Json.Serialization.JsonPropertyName("setup")]
    public bool Setup { get => _setup; set => _setup = value; }

    public AuthState()
    {
        _setup = false;
    }
}

public class ParsedSettings
{
    private string _startTime;
    private int _timeMinutes;
    private bool _visible;

    public string StartTime { get => _startTime; set => _startTime = value; }
    public int TimeMinutes { get => _timeMinutes; set => _timeMinutes = value; }
    public bool Visible { get => _visible; set => _visible = value; }
    public string DisplayTime
    {
        get
        {
            if (_startTime.Length > 0 && DateTime.TryParse(_startTime, out DateTime dt))
            {
                return dt.ToString("dd.MM.yyyy HH:mm");
            }
            return "";
        }
    }

    public ParsedSettings()
    {
        _startTime = "";
        _timeMinutes = 0;
        _visible = true;
    }
}

public class ExportRequest
{
    private List<string> _fileNames;

    public List<string> FileNames { get => _fileNames; set => _fileNames = value; }

    public ExportRequest()
    {
        _fileNames = new List<string>();
    }
}
