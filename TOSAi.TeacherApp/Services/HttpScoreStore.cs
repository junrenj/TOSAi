using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public interface IScoreStore
{
    string Description { get; }

    Task SaveAsync(IEnumerable<ScoreImportRow> rows, CancellationToken cancellationToken = default);

    Task<ObservableCollection<ScoreImportRow>> LoadAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class HttpScoreStore : IScoreStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpScoreStore(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public string Description => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "云端 API";

    public async Task SaveAsync(IEnumerable<ScoreImportRow> rows, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/scores/import-rows", rows.ToList(), cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"云端保存失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }
    }

    public async Task<ObservableCollection<ScoreImportRow>> LoadAsync(CancellationToken cancellationToken = default)
    {
        ScoreImportRowsResponse? response = await _httpClient.GetFromJsonAsync<ScoreImportRowsResponse>("api/scores/import-rows", JsonOptions, cancellationToken);
        return response?.Rows is null ? [] : new ObservableCollection<ScoreImportRow>(response.Rows);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync("api/scores/import-rows", cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"云端清空失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }
    }

    private sealed class ScoreImportRowsResponse
    {
        public List<ScoreImportRow>? Rows { get; set; }

        public int Count { get; set; }

        public string Storage { get; set; } = string.Empty;
    }
}
