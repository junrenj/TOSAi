using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public interface IQuestionDraftStore
{
    Task<ObservableCollection<QuestionDraft>> LoadAsync(CancellationToken cancellationToken = default);

    Task<QuestionDraft> SaveAsync(QuestionDraft draft, CancellationToken cancellationToken = default);

    Task<QuestionDraft> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class HttpQuestionDraftStore : IQuestionDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpQuestionDraftStore(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
        AuthSession.Apply(_httpClient);
    }

    public async Task<ObservableCollection<QuestionDraft>> LoadAsync(CancellationToken cancellationToken = default)
    {
        QuestionDraftRowsResponse? response = await _httpClient.GetFromJsonAsync<QuestionDraftRowsResponse>(
            "api/questions/drafts",
            JsonOptions,
            cancellationToken);
        return response?.Rows is null ? [] : new ObservableCollection<QuestionDraft>(response.Rows);
    }

    public async Task<QuestionDraft> SaveAsync(QuestionDraft draft, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/questions/drafts", draft, cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Question draft save failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        QuestionDraft? saved = JsonSerializer.Deserialize<QuestionDraft>(responseText, JsonOptions);
        return saved ?? throw new InvalidOperationException("The server did not return a question draft.");
    }

    public async Task<QuestionDraft> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PatchAsJsonAsync(
            $"api/questions/drafts/{Uri.EscapeDataString(id)}/status",
            new QuestionDraftStatusUpdateRequest(status),
            cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Question draft status update failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        QuestionDraft? saved = JsonSerializer.Deserialize<QuestionDraft>(responseText, JsonOptions);
        return saved ?? throw new InvalidOperationException("The server did not return a question draft.");
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"api/questions/drafts/{Uri.EscapeDataString(id)}", cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Question draft delete failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }
    }

    private sealed class QuestionDraftRowsResponse
    {
        public List<QuestionDraft>? Rows { get; set; }

        public int Count { get; set; }

        public string Storage { get; set; } = string.Empty;
    }

    private sealed record QuestionDraftStatusUpdateRequest(string Status);
}
