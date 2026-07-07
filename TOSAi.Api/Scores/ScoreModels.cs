namespace TOSAi.Api;

sealed record ScoreImportRowsResponse(IReadOnlyList<ScoreImportRowDto> Rows, int Count, string Storage);

sealed record ScoreImportRowDto(
    string ExamName,
    DateOnly ExamDate,
    string GradeName,
    string ClassName,
    string StudentId,
    string StudentName,
    string SubjectName,
    double Score,
    double FullScore)
{
    public double ScoreRate => FullScore <= 0 ? 0 : Math.Round(Score / FullScore * 100, 1);
}
