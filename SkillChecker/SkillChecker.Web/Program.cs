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

app.MapGet("/api/tests", () =>
{
    Directory.CreateDirectory(testsFolder);
    string[] files = Directory.GetFiles(testsFolder, "*.json");
    List<object> list = new List<object>();
    for (int i = 0; i < files.Length; i++)
    {
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
