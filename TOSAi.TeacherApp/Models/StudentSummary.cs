namespace TOSAi.TeacherApp.Models;

public sealed class StudentSummary
{
    public required string StudentId { get; init; }

    public required string Name { get; init; }

    public required string ClassName { get; init; }

    public int TotalScore { get; init; }

    public int GradeRank { get; init; }

    public required string StrongSubject { get; init; }

    public required string WeakSubject { get; init; }

    public required string Advice { get; init; }
}
