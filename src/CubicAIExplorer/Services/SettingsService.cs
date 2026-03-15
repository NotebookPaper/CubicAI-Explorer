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
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var fallbackPath = Path.Combine(appData, "CubicAIExplorer", "settings.json");
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return PathSecurityHelper.SanitizePathOrFallback(overridePath, fallbackPath);

        return fallbackPath;
    }

    public UserSettings Load()
    {
        var path = GetSettingsPath();
        return LoadCore(path);
    }

    public Task<UserSettings> LoadAsync()
    {
        var path = GetSettingsPath();
        return LoadCoreAsync(path);
    }

    public void Save(UserSettings settings)
    {
        SaveCore(settings);
    }

    public Task SaveAsync(UserSettings settings)
    {
        return SaveCoreAsync(settings);
    }

    private static UserSettings LoadCore(string path)
    {
        if (!File.Exists(path))
            return new UserSettings();

        for (var i = 0; i < 3; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (IOException) when (i < 2)
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

    private static async Task<UserSettings> LoadCoreAsync(string path)
    {
        if (!File.Exists(path))
            return new UserSettings();

        for (var i = 0; i < 3; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (IOException) when (i < 2)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            catch
            {
                break;
            }
        }

        return new UserSettings();
    }

    private void SaveCore(UserSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var suppression = _watcher?.SuppressNotifications();

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch
        {
            // Non-critical.
        }
    }

    private async Task SaveCoreAsync(UserSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var suppression = _watcher?.SuppressNotifications();

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(json).ConfigureAwait(false);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            // Non-critical.
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
