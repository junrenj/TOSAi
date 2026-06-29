using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TOSAi.TeacherApp.Services;

public sealed class OpenAiCompatibleAnalysisService : IAiAnalysisService
{
    private readonly AiSettings _settings;
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleAnalysisService(AiSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (_settings.UseMockAnalysis)
        {
            throw new InvalidOperationException("AI 接口尚未配置完整，请先在系统设置中填写接口地址、模型名称和 API Key。");
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, BuildChatCompletionsUri(_settings.Endpoint));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(new ChatCompletionRequest(
            _settings.Model,
            [
                new ChatMessage("system", "你是一名服务教师的成绩分析助手。请基于输入的成绩数据，输出可执行、克制、具体的教学分析。"),
                new ChatMessage("user", $"分析范围：{request.Scope}\n\n{request.Prompt}")
            ],
            0.2));

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI 接口返回失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        ChatCompletionResponse? completion = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(responseText);
        string content = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI 接口没有返回可用内容。");
        }

        return new AiAnalysisResult("真实 AI 接口已返回分析结果。", content);
    }

    private static string BuildChatCompletionsUri(string endpoint)
    {
        string normalizedEndpoint = endpoint.Trim().TrimEnd('/');
        return normalizedEndpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? normalizedEndpoint
            : $"{normalizedEndpoint}/chat/completions";
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] List<ChatChoice>? Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}
