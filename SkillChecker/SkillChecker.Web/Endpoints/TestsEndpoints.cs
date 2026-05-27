using System.Text;
using System.Text.Json;
using SkillChecker.Common.Models;
using SkillChecker.Web.Models;

namespace SkillChecker.Web.Endpoints;

public static class TestsEndpoints
{
    private static string _testsFolder = "";

    public static void MapTestsEndpoints(this WebApplication app, string testsFolder)
    {
        _testsFolder = testsFolder;

        app.MapGet("/api/tests", () =>
        {
            System.IO.Directory.CreateDirectory(_testsFolder);
            string[] files = System.IO.Directory.GetFiles(_testsFolder, "*.json");
            List<object> list = new List<object>();

            Dictionary<string, JsonElement> settingsData = SettingsEndpoints.LoadSettingsRaw();

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
                    parsed = SettingsEndpoints.ParseTestSettings(settingsElem);
                    hasSettings = settingsElem.ValueKind == JsonValueKind.Object;
                }

                list.Add(new TestListItem { Name = name, QuestionCount = count, Visible = parsed.Visible, HasSettings = hasSettings, DisplayTime = parsed.DisplayTime, TimeMinutes = parsed.TimeMinutes });
            }
            return Results.Json(list);
        });

        app.MapGet("/api/test/{name}/preview", (string name) =>
        {
            string filePath = Path.Combine(_testsFolder, name + ".json");
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
            string filePath = Path.Combine(_testsFolder, name + ".json");
            if (!File.Exists(filePath))
            {
                return Results.NotFound(new ErrorResult { Error = "Тест не найден" });
            }

            File.Delete(filePath);
            return Results.Json(new OperationResult { Ok = true });
        });

        app.MapPost("/api/upload", async (HttpContext context) =>
        {
            System.IO.Directory.CreateDirectory(_testsFolder);
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

            string filePath = Path.Combine(_testsFolder, name + ".json");
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Results.Json(new OperationResult { Ok = true, Name = name });
        });
    }
}
