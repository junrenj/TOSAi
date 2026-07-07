namespace TOSAi.Api;

sealed record ReportDraftRowsResponse(IReadOnlyList<ReportDraftDto> Rows, int Count, string Storage);

sealed record ReportDraftDto(
    string Id,
    DateTimeOffset CreatedAt,
    string Scope,
    string Prompt,
    string Summary,
    string Suggestions);
