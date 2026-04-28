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
string scheduleFile = Path.Combine(testsFolder, "schedule.json");

app.MapGet("/api/tests", () =>
{
    Directory.CreateDirectory(testsFolder);
    string[] files = Directory.GetFiles(testsFolder, "*.json");
    List<object> list = new List<object>();
    for (int i = 0; i < files.Length; i++)
    {
        if (Path.GetFileName(files[i]) == "schedule.json") continue;

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

app.MapGet("/api/schedule", () =>
{
    if (!File.Exists(scheduleFile))
    {
        return Results.Json(new Dictionary<string, string>());
    }

    string json = File.ReadAllText(scheduleFile, Encoding.UTF8);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
    if (data == null)
    {
        return Results.Json(new Dictionary<string, string>());
    }

    List<object> list = new List<object>();
    foreach (var kvp in data)
    {
        if (DateTime.TryParse(kvp.Value, out DateTime dt))
        {
            list.Add(new { TestName = kvp.Key, ScheduledTime = dt.ToString("o"), DisplayTime = dt.ToString("dd.MM.yyyy HH:mm") });
        }
    }
    return Results.Json(list);
});

app.MapPost("/api/schedule", (ScheduleRequest req) =>
{
    Directory.CreateDirectory(testsFolder);

    Dictionary<string, string> data = new Dictionary<string, string>();
    if (File.Exists(scheduleFile))
    {
        string existingJson = File.ReadAllText(scheduleFile, Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Dictionary<string, string>? existing = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson, options);
        if (existing != null)
        {
            foreach (var kvp in existing)
            {
                data[kvp.Key] = kvp.Value;
            }
        }
    }

    if (DateTime.TryParse(req.Time, out DateTime scheduled))
    {
        data[req.TestName] = scheduled.ToString("o");
    }

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    string json = JsonSerializer.Serialize(data, jsonOptions);
    File.WriteAllText(scheduleFile, json, Encoding.UTF8);

    return Results.Json(new { ok = true });
});

app.MapDelete("/api/schedule/{testName}", (string testName) =>
{
    if (!File.Exists(scheduleFile))
    {
        return Results.Json(new { ok = true });
    }

    string existingJson = File.ReadAllText(scheduleFile, Encoding.UTF8);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson, options);
    if (data != null && data.ContainsKey(testName))
    {
        data.Remove(testName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string json = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(scheduleFile, json, Encoding.UTF8);
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

public class ScheduleRequest
{
    public string TestName { get; set; } = "";
    public string Time { get; set; } = "";
}
