using SkillChecker.Common.Security;
using SkillChecker.Web.Models;

namespace SkillChecker.Web.Endpoints;

public static class AuthEndpoints
{
    private static AuthData? _cachedAuth;
    private static string _authFile = "";

    public static void MapAuthEndpoints(this WebApplication app, string authFile)
    {
        _authFile = authFile;

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
    }

    public static AuthData? LoadAuthData()
    {
        if (!File.Exists(_authFile)) return null;
        try
        {
            string json = File.ReadAllText(_authFile, System.Text.Encoding.UTF8);
            System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _cachedAuth = System.Text.Json.JsonSerializer.Deserialize<AuthData>(json, options);
            return _cachedAuth;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsAuthSetup()
    {
        AuthData? data = LoadAuthData();
        return data != null && data.PasswordHash.Length > 0;
    }

    public static bool VerifyPassword(string password, out bool needsMigration)
    {
        needsMigration = false;
        AuthData? data = LoadAuthData();
        if (data == null || data.PasswordHash.Length == 0) return false;
        return PasswordHasher.Verify(password, data.PasswordHash, out needsMigration);
    }

    public static void SaveAuth(string passwordHash)
    {
        string? dir = Path.GetDirectoryName(_authFile);
        if (dir != null) System.IO.Directory.CreateDirectory(dir);
        AuthData data = new AuthData();
        data.PasswordHash = passwordHash;
        System.Text.Json.JsonSerializerOptions jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_authFile, System.Text.Json.JsonSerializer.Serialize(data, jsonOptions), System.Text.Encoding.UTF8);
    }

    public static bool IsAuthorized(HttpContext context)
    {
        string? password = context.Request.Headers["X-Auth-Password"];
        if (string.IsNullOrEmpty(password)) return false;
        bool ignored;
        return VerifyPassword(password, out ignored);
    }
}
