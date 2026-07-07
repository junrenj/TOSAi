using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public interface IReportDraftStore
{
    Task<ObservableCollection<ReportDraft>> LoadAsync(CancellationToken cancellationToken = default);

    Task<ReportDraft> SaveAsync(ReportDraft draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class HttpReportDraftStore : IReportDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpReportDraftStore(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
        AuthSession.Apply(_httpClient);
    }

    public async Task<ObservableCollection<ReportDraft>> LoadAsync(CancellationToken cancellationToken = default)
    {
        ReportDraftRowsResponse? response = await _httpClient.GetFromJsonAsync<ReportDraftRowsResponse>(
            "api/reports/drafts",
            JsonOptions,
            cancellationToken);
        return response?.Rows is null ? [] : new ObservableCollection<ReportDraft>(response.Rows);
    }

    public async Task<ReportDraft> SaveAsync(ReportDraft draft, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/reports/drafts", draft, cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"报告草稿保存失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        ReportDraft? saved = JsonSerializer.Deserialize<ReportDraft>(responseText, JsonOptions);
        return saved ?? throw new InvalidOperationException("服务器没有返回报告草稿。");
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"api/reports/drafts/{Uri.EscapeDataString(id)}", cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"报告草稿删除失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }
    }

    private sealed class ReportDraftRowsResponse
    {
        public List<ReportDraft>? Rows { get; set; }

        public int Count { get; set; }

        public string Storage { get; set; } = string.Empty;
    }
}