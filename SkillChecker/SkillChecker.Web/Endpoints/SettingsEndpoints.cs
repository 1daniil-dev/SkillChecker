using System.Text;
using System.Text.Json;
using SkillChecker.Web.Models;

namespace SkillChecker.Web.Endpoints;

public static class SettingsEndpoints
{
    private static string _settingsFile = "";
    private static string _testsFolder = "";

    public static void MapSettingsEndpoints(this WebApplication app, string testsFolder)
    {
        _testsFolder = testsFolder;
        _settingsFile = Path.Combine(testsFolder, "test_settings.json");

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
            System.IO.Directory.CreateDirectory(_testsFolder);

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
            if (!File.Exists(_settingsFile))
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
            System.IO.Directory.CreateDirectory(_testsFolder);

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
    }

    public static ParsedSettings ParseTestSettings(JsonElement elem)
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

    public static Dictionary<string, JsonElement> LoadSettingsRaw()
    {
        Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>();
        if (File.Exists(_settingsFile))
        {
            string json = File.ReadAllText(_settingsFile, Encoding.UTF8);
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Dictionary<string, JsonElement>? loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            if (loaded != null)
            {
                result = loaded;
            }
        }
        return result;
    }

    private static Dictionary<string, object> LoadSettingsAsEntries()
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

    private static void SaveSettingsData(Dictionary<string, object> data)
    {
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string outJson = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(_settingsFile, outJson, Encoding.UTF8);
    }
}
