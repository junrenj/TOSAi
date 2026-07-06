namespace TOSAi.TeacherApp.Services;

public static class ApiEndpointOptions
{
    public const string EnvironmentVariableName = "TOSAI_API_BASE_URL";
    public const string DefaultBaseUrl = "http://localhost:5088";

    public static string BaseUrl => Normalize(Environment.GetEnvironmentVariable(EnvironmentVariableName)) ?? DefaultBaseUrl;

    private static string? Normalize(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return baseUrl.Trim().TrimEnd('/');
    }
}