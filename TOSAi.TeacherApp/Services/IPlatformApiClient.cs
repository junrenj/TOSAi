using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public interface IPlatformApiClient
{
    Task<PlatformFeaturePage> GetFeaturePageAsync(UserRole role, string pageKey, CancellationToken cancellationToken = default);
}
