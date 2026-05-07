using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;
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

if (!Directory.Exists(testsFolder))
{
    testsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests");
    resultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
    settingsFile = Path.Combine(testsFolder, "test_settings.json");
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
    Directory.CreateDirectory(resultsFolder);
    string[] files = Directory.GetFiles(resultsFolder, "*.json");
    List<object> list = new List<object>();
    for (int i = 0; i < files.Length; i++)
    {
        string json = File.ReadAllText(files[i], Encoding.UTF8);
        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        TestResult? result = JsonSerializer.Deserialize<TestResult>(json, options);
        if (result != null)
        {
            ResultListItem item = new ResultListItem();
            item.StudentName = result.StudentName;
            item.Group = result.Group;
            item.TestName = result.TestName;
            item.Date = result.Date;
            item.TotalQuestions = result.TotalQuestions;
            item.CorrectAnswers = result.CorrectAnswers;
            item.Score = result.Score;
            item.Answers = result.Answers;
            item.FileName = Path.GetFileName(files[i]);
            list.Add(item);
        }
    }
    return Results.Json(list);
});

app.MapDelete("/api/results/{fileName}", (string fileName) =>
{
    string filePath = Path.Combine(resultsFolder, fileName);
    if (!File.Exists(filePath))
    {
        return Results.NotFound(new ErrorResult { Error = "Результат не найден" });
    }

    File.Delete(filePath);
    return Results.Json(new OperationResult { Ok = true });
});

app.MapDelete("/api/results", () =>
{
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
