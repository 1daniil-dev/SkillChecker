using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillChecker.Data;
using SkillChecker.Web.Endpoints;
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
string authFile = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "auth.json");
string dbPath = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "skillchecker.db");

if (!Directory.Exists(testsFolder))
{
    testsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tests");
    resultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
    authFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "auth.json");
    dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "skillchecker.db");
}

string? dbDir = Path.GetDirectoryName(dbPath);
if (dbDir != null) Directory.CreateDirectory(dbDir);
using (AppDbContext initDb = new AppDbContext(dbPath))
{
    initDb.Database.EnsureCreated();
}

app.Use(async (HttpContext context, Func<Task> next) =>
{
    string path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/api/") && !path.StartsWith("/api/auth/"))
    {
        if (!AuthEndpoints.IsAuthorized(context))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new ErrorResult { Error = "Не авторизован" });
            return;
        }
    }
    await next();
});

app.MapAuthEndpoints(authFile);
app.MapTestsEndpoints(testsFolder);
app.MapResultsEndpoints(dbPath, resultsFolder);
app.MapSettingsEndpoints(testsFolder);

app.Run();
