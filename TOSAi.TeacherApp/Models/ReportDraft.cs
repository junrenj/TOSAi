namespace TOSAi.TeacherApp.Models;

public sealed class ReportDraft
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Suggestions { get; set; } = string.Empty;

    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string ShortSummary => string.IsNullOrWhiteSpace(Summary) ? "未生成摘要" : Summary;
}