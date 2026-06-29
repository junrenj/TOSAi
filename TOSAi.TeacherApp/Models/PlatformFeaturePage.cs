namespace TOSAi.TeacherApp.Models;

public sealed class PlatformFeaturePage
{
    public required IReadOnlyList<PlatformMetricCard> Cards { get; init; }

    public required IReadOnlyList<PlatformActivityItem> Activities { get; init; }

    public string Note { get; init; } = string.Empty;
}

public sealed class PlatformMetricCard
{
    public required string Label { get; init; }

    public required string Value { get; init; }

    public required string Hint { get; init; }
}

public sealed class PlatformActivityItem
{
    public required string Title { get; init; }

    public required string Category { get; init; }

    public required string TimeText { get; init; }

    public required string Detail { get; init; }

    public required string Status { get; init; }
}
