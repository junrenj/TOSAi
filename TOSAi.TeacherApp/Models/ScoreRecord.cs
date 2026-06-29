namespace TOSAi.TeacherApp.Models;

public sealed class ScoreRecord
{
    public required string StudentName { get; init; }

    public required string ClassName { get; init; }

    public double Chinese { get; init; }

    public double Math { get; init; }

    public double English { get; init; }

    public double Physics { get; init; }

    public double Chemistry { get; init; }

    public double Total => Chinese + Math + English + Physics + Chemistry;

    public double Average => System.Math.Round(Total / 5, 1);
}

