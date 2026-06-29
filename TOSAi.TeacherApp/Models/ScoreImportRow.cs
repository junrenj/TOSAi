namespace TOSAi.TeacherApp.Models;

public sealed class ScoreImportRow
{
    public required string ExamName { get; init; }

    public required DateOnly ExamDate { get; init; }

    public required string GradeName { get; init; }

    public required string ClassName { get; init; }

    public required string StudentId { get; init; }

    public required string StudentName { get; init; }

    public required string SubjectName { get; init; }

    public double Score { get; init; }

    public double FullScore { get; init; }

    public double ScoreRate => FullScore <= 0 ? 0 : System.Math.Round(Score / FullScore * 100, 1);
}
