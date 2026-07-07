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
