using System.Collections.ObjectModel;

namespace TOSAi.TeacherApp.Services;

public sealed record QuestionGenerationRequest(
    string Difficulty,
    IReadOnlyList<string> Topics,
    IReadOnlyList<string> Directions,
    IReadOnlyList<string> Scenarios,
    int ChoiceCount,
    int EssayCount,
    IReadOnlyList<QuestionGenerationReference> References);

public sealed record QuestionGenerationReference(
    string Type,
    string Topic,
    string Direction,
    string Scenario,
    string Difficulty,
    string Stem,
    IReadOnlyList<string> Options,
    string Answer,
    string Explanation);

public sealed record GeneratedQuestionDraft(
    string Type,
    string Difficulty,
    string Topic,
    string Direction,
    string Scenario,
    string Stem,
    IReadOnlyList<string> Options,
    string Answer);

public interface IQuestionGenerationService
{
    Task<IReadOnlyList<GeneratedQuestionDraft>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken = default);
}