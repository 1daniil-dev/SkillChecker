using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;

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
string resultsFolder = Path.Combine(solutionDir, "Results");
string settingsFile = Path.Combine(testsFolder, "test_settings.json");

app.MapGet("/api/tests", () =>
{
    Directory.CreateDirectory(testsFolder);
    string[] files = Directory.GetFiles(testsFolder, "*.json");
    List<object> list = new List<object>();
    for (int i = 0; i < files.Length; i++)
    {
        string fileName = Path.GetFileName(files[i]);
        if (fileName == "test_settings.json" || fileName == "schedule.json") continue;

        string name = Path.GetFileNameWithoutExtension(files[i]);
        string json = File.ReadAllText(files[i], Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(json, options);
        int count = questions != null ? questions.Count : 0;
        list.Add(new { Name = name, QuestionCount = count });
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        TestResult? result = JsonSerializer.Deserialize<TestResult>(json, options);
        if (result != null)
        {
            list.Add(result);
        }
    }
    return Results.Json(list);
});

app.MapGet("/api/settings", () =>
{
    if (!File.Exists(settingsFile))
    {
        return Results.Json(new List<object>());
    }

    string json = File.ReadAllText(settingsFile, Encoding.UTF8);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    Dictionary<string, JsonElement>? data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
    if (data == null)
    {
        return Results.Json(new List<object>());
    }

    List<object> list = new List<object>();
    foreach (var kvp in data)
    {
        string startTime = "";
        int timeMinutes = 0;

        if (kvp.Value.ValueKind == JsonValueKind.Object)
        {
            JsonElement startElem;
            if (kvp.Value.TryGetProperty("StartTime", out startElem) && startElem.ValueKind == JsonValueKind.String)
            {
                startTime = startElem.GetString() ?? "";
            }

            JsonElement timeElem;
            if (kvp.Value.TryGetProperty("TimeMinutes", out timeElem) && timeElem.ValueKind == JsonValueKind.Number)
            {
                timeMinutes = timeElem.GetInt32();
            }
        }
        else if (kvp.Value.ValueKind == JsonValueKind.String)
        {
            startTime = kvp.Value.GetString() ?? "";
        }

        string displayTime = "";
        if (startTime.Length > 0 && DateTime.TryParse(startTime, out DateTime dt))
        {
            displayTime = dt.ToString("dd.MM.yyyy HH:mm");
        }

        list.Add(new { TestName = kvp.Key, StartTime = startTime, DisplayTime = displayTime, TimeMinutes = timeMinutes });
    }
    return Results.Json(list);
});

app.MapPost("/api/settings", (SettingsRequest req) =>
{
    Directory.CreateDirectory(testsFolder);

    Dictionary<string, object> data = new Dictionary<string, object>();
    if (File.Exists(settingsFile))
    {
        string existingJson = File.ReadAllText(settingsFile, Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Dictionary<string, JsonElement>? existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson, options);
        if (existing != null)
        {
            foreach (var kvp in existing)
            {
                if (kvp.Value.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, object> entry = new Dictionary<string, object>();
                    JsonElement startElem;
                    string st = "";
                    if (kvp.Value.TryGetProperty("StartTime", out startElem) && startElem.ValueKind == JsonValueKind.String)
                    {
                        st = startElem.GetString() ?? "";
                    }
                    entry["StartTime"] = st;

                    JsonElement timeElem;
                    int tm = 0;
                    if (kvp.Value.TryGetProperty("TimeMinutes", out timeElem) && timeElem.ValueKind == JsonValueKind.Number)
                    {
                        tm = timeElem.GetInt32();
                    }
                    entry["TimeMinutes"] = tm;

                    data[kvp.Key] = entry;
                }
                else if (kvp.Value.ValueKind == JsonValueKind.String)
                {
                    Dictionary<string, object> entry = new Dictionary<string, object>();
                    entry["StartTime"] = kvp.Value.GetString() ?? "";
                    entry["TimeMinutes"] = 0;
                    data[kvp.Key] = entry;
                }
            }
        }
    }

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
    data[req.TestName] = newEntry;

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    string outJson = JsonSerializer.Serialize(data, jsonOptions);
    File.WriteAllText(settingsFile, outJson, Encoding.UTF8);

    return Results.Json(new { ok = true });
});

app.MapDelete("/api/settings/{testName}", (string testName) =>
{
    if (!File.Exists(settingsFile))
    {
        return Results.Json(new { ok = true });
    }

    string existingJson = File.ReadAllText(settingsFile, Encoding.UTF8);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    Dictionary<string, JsonElement>? data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson, options);
    if (data != null && data.ContainsKey(testName))
    {
        data.Remove(testName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string outJson = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(settingsFile, outJson, Encoding.UTF8);
    }

    return Results.Json(new { ok = true });
});

app.MapPost("/api/upload", async (HttpContext context) =>
{
    Directory.CreateDirectory(testsFolder);
    var form = await context.Request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "Файл не выбран" });
    }

    string name = form["name"].ToString();
    if (name.Length == 0)
    {
        name = Path.GetFileNameWithoutExtension(file.FileName);
    }

    string filePath = Path.Combine(testsFolder, name + ".json");
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Json(new { ok = true, name = name });
});

app.MapDelete("/api/test/{name}", (string name) =>
{
    string filePath = Path.Combine(testsFolder, name + ".json");
    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { error = "Тест не найден" });
    }

    File.Delete(filePath);
    return Results.Json(new { ok = true });
});

app.Run();

public class SettingsRequest
{
    public string TestName { get; set; } = "";
    public string StartTime { get; set; } = "";
    public int TimeMinutes { get; set; } = 0;
}
