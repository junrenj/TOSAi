namespace TOSAi.TeacherApp.Models;

public sealed class SubjectInsight
{
    public required string SubjectName { get; init; }

    public double ClassAverage { get; init; }

    public double GradeAverage { get; init; }

    public int AdvantageCount { get; init; }

    public int RiskCount { get; init; }

    public required string Comment { get; init; }
}
