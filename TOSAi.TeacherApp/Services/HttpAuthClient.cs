using System.Net.Http;
using System.Net.Http.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public sealed class HttpAuthClient
{
    private readonly HttpClient _httpClient;

    public HttpAuthClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<LoginResponse> LoginAsync(string role, string username, string password, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest(role, username, password),
            cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"登录失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        return login ?? throw new InvalidOperationException("服务器没有返回登录信息。");
    }

    private sealed record LoginRequest(string Role, string Username, string Password);
}