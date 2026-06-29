using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public sealed class FallbackPlatformApiClient : IPlatformApiClient
{
    private readonly IPlatformApiClient _primary;
    private readonly IPlatformApiClient _fallback;

    public FallbackPlatformApiClient(IPlatformApiClient primary, IPlatformApiClient fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<PlatformFeaturePage> GetFeaturePageAsync(UserRole role, string pageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetFeaturePageAsync(role, pageKey, cancellationToken);
        }
        catch
        {
            return await _fallback.GetFeaturePageAsync(role, pageKey, cancellationToken);
        }
    }
}
