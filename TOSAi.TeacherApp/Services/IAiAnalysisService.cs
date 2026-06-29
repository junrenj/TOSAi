namespace TOSAi.TeacherApp.Services;

public sealed record AiAnalysisRequest(string Scope, string Prompt);

public sealed record AiAnalysisResult(string Summary, string Suggestions);

public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default);
}
