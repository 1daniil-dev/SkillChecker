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
