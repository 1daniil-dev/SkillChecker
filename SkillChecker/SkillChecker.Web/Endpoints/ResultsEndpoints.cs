using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SkillChecker.Common.Models;
using SkillChecker.Data;
using SkillChecker.Web.Models;

namespace SkillChecker.Web.Endpoints;

public static class ResultsEndpoints
{
    private static string _dbPath = "";
    private static string _resultsFolder = "";

    public static void MapResultsEndpoints(this WebApplication app, string dbPath, string resultsFolder)
    {
        _dbPath = dbPath;
        _resultsFolder = resultsFolder;

        SyncResultsToDb();

        app.MapGet("/api/results", () =>
        {
            SyncResultsToDb();
            using (AppDbContext db = new AppDbContext(_dbPath))
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
            using (AppDbContext db = new AppDbContext(_dbPath))
            {
                ResultEntity? entity = db.Results.FirstOrDefault(r => r.SourceFile == safe);
                if (entity != null)
                {
                    db.Results.Remove(entity);
                    db.SaveChanges();
                }
            }
            string filePath = Path.Combine(_resultsFolder, safe);
            if (File.Exists(filePath)) File.Delete(filePath);
            return Results.Json(new OperationResult { Ok = true });
        });

        app.MapDelete("/api/results", () =>
        {
            using (AppDbContext db = new AppDbContext(_dbPath))
            {
                List<ResultEntity> all = db.Results.ToList();
                for (int i = 0; i < all.Count; i++)
                {
                    db.Results.Remove(all[i]);
                }
                db.SaveChanges();
            }
            if (System.IO.Directory.Exists(_resultsFolder))
            {
                string[] files = System.IO.Directory.GetFiles(_resultsFolder, "*.json");
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }
            return Results.Json(new OperationResult { Ok = true });
        });

        app.MapPost("/api/results/export", async (HttpContext context) =>
        {
            if (!AuthEndpoints.IsAuthorized(context)) return Results.Unauthorized();

            using StreamReader reader = new StreamReader(context.Request.Body);
            string body = await reader.ReadToEndAsync();
            JsonSerializerOptions jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ExportRequest? req = JsonSerializer.Deserialize<ExportRequest>(body, jsonOpts);
            if (req == null || req.FileNames.Count == 0)
            {
                return Results.BadRequest(new ErrorResult { Error = "Нет выбранных результатов" });
            }

            List<TestResult> results = new List<TestResult>();
            using (AppDbContext db = new AppDbContext(_dbPath))
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
    }

    private static void SyncResultsToDb()
    {
        if (!System.IO.Directory.Exists(_resultsFolder)) return;
        string[] files = System.IO.Directory.GetFiles(_resultsFolder, "*.json");
        using (AppDbContext db = new AppDbContext(_dbPath))
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
}
