using System.IO;
using System.Text.Json;

namespace TOSAi.TeacherApp.Services;

public sealed class AiSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public AiSettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dataDirectory = Path.Combine(appData, "TOSAi.TeacherApp");
        Directory.CreateDirectory(dataDirectory);
        _settingsFilePath = Path.Combine(dataDirectory, "ai-settings.json");
    }

    public string SettingsFilePath => _settingsFilePath;

    public async Task<AiSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AiSettings();
        }

        await using FileStream stream = File.OpenRead(_settingsFilePath);
        return await JsonSerializer.DeserializeAsync<AiSettings>(stream, JsonOptions, cancellationToken) ?? new AiSettings();
    }

    public async Task SaveAsync(AiSettings settings, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
