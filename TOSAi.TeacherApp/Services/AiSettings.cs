namespace TOSAi.TeacherApp.Services;

public sealed class AiSettings
{
    public string Provider { get; set; } = "暂不接入（模拟分析）";

    public string Endpoint { get; set; } = "https://api.example.com/v1";

    public string Model { get; set; } = "your-model-name";

    public string ApiKey { get; set; } = string.Empty;

    public bool UseMockAnalysis => Provider.StartsWith("暂不接入", StringComparison.Ordinal) ||
                                   string.IsNullOrWhiteSpace(Endpoint) ||
                                   string.IsNullOrWhiteSpace(Model) ||
                                   string.IsNullOrWhiteSpace(ApiKey);
}
