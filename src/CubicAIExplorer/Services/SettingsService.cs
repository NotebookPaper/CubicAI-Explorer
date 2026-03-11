using System.IO;
using System.Text.Json;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class SettingsService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly DebouncedJsonFileWatcher<UserSettings>? _watcher;

    public event EventHandler<UserSettings>? SettingsChanged;

    public SettingsService()
    {
        var path = GetSettingsPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _watcher = new DebouncedJsonFileWatcher<UserSettings>(
                path,
                Load,
                settings => SettingsChanged?.Invoke(this, settings));
        }
    }

    public static string GetSettingsPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "settings.json");
    }

    public UserSettings Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path)) return new UserSettings();

        for (int i = 0; i < 3; i++) // Retry a few times if file is busy (syncing)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch
            {
                break;
            }
        }
        return new UserSettings();
    }

    public void Save(UserSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var suppression = _watcher?.SuppressNotifications();

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch { /* Non-critical */ }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
