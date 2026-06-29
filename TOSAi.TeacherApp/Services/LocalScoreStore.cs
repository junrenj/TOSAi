using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public sealed class LocalScoreStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dataFilePath;

    public LocalScoreStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dataDirectory = Path.Combine(appData, "TOSAi.TeacherApp");
        Directory.CreateDirectory(dataDirectory);
        _dataFilePath = Path.Combine(dataDirectory, "score-import-rows.json");
    }

    public string DataFilePath => _dataFilePath;

    public async Task SaveAsync(IEnumerable<ScoreImportRow> rows, CancellationToken cancellationToken = default)
    {
        ScoreStoreDocument document = new(DateTimeOffset.Now, rows.ToList());
        await using FileStream stream = File.Create(_dataFilePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    public async Task<ObservableCollection<ScoreImportRow>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_dataFilePath))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(_dataFilePath);
        ScoreStoreDocument? document = await JsonSerializer.DeserializeAsync<ScoreStoreDocument>(stream, JsonOptions, cancellationToken);
        return document?.Rows is null ? [] : new ObservableCollection<ScoreImportRow>(document.Rows);
    }

    public Task ClearAsync()
    {
        if (File.Exists(_dataFilePath))
        {
            File.Delete(_dataFilePath);
        }

        return Task.CompletedTask;
    }

    private sealed record ScoreStoreDocument(DateTimeOffset SavedAt, List<ScoreImportRow> Rows);
}
