namespace TOSAi.Api;

sealed record PlatformFeaturePage(IReadOnlyList<PlatformMetricCard> Cards, IReadOnlyList<PlatformActivityItem> Activities, string Note);

sealed record PlatformMetricCard(string Label, string Value, string Hint);

sealed record PlatformActivityItem(string Title, string Category, string TimeText, string Detail, string Status);
