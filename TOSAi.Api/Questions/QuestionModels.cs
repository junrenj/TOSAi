namespace TOSAi.Api;

sealed record QuestionBankRowsResponse(IReadOnlyList<QuestionBankRowDto> Rows, int Count, string Storage);

sealed record QuestionBankRowDto(
    string Type,
    string Topic,
    string Direction,
    string Scenario,
    string Difficulty,
    string Stem,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    string Answer,
    string Explanation);

sealed record QuestionDraftRowsResponse(IReadOnlyList<QuestionDraftDto> Rows, int Count, string Storage);

sealed record QuestionDraftStatusUpdateRequest(string Status);

sealed record QuestionDraftDto(
    string Id,
    DateTimeOffset CreatedAt,
    string Status,
    string Type,
    string Topic,
    string Direction,
    string Scenario,
    string Difficulty,
    string Stem,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    string Answer,
    string SourcePrompt,
    int ReferenceCount);
