using System.Net.Http;
using System.Net.Http.Json;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public sealed class HttpPlatformApiClient : IPlatformApiClient
{
    private readonly HttpClient _httpClient;

    public HttpPlatformApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        AuthSession.Apply(_httpClient);
    }

    public async Task<PlatformFeaturePage> GetFeaturePageAsync(UserRole role, string pageKey, CancellationToken cancellationToken = default)
    {
        PlatformFeaturePage? page = await _httpClient.GetFromJsonAsync<PlatformFeaturePage>(
            $"api/platform/{role}/{pageKey}",
            cancellationToken);

        return page ?? throw new InvalidOperationException("服务器没有返回页面数据。");
    }
}
