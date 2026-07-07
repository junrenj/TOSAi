namespace TOSAi.Api;

internal static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/questions/import-rows", async (HttpRequest request, IQuestionBankRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            IReadOnlyList<QuestionBankRowDto> rows = await store.LoadAsync(cancellationToken);
            return Results.Ok(new QuestionBankRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapGet("/api/questions", async (HttpRequest request, IQuestionBankRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            IReadOnlyList<QuestionBankRowDto> rows = await store.LoadAsync(cancellationToken);
            return Results.Ok(new QuestionBankRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapPost("/api/questions/import-rows", async (HttpRequest request, IReadOnlyList<QuestionBankRowDto> rows, IQuestionBankRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            if (rows.Count > 0)
            {
                string? validationError = ValidateQuestionRows(rows);
                if (validationError is not null)
                {
                    return Results.BadRequest(new { message = validationError });
                }
            }

            await store.SaveAsync(rows, cancellationToken);
            return Results.Ok(new QuestionBankRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapDelete("/api/questions/import-rows", async (HttpRequest request, IQuestionBankRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            await store.ClearAsync(cancellationToken);
            return Results.Ok(new QuestionBankRowsResponse([], 0, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        return app;
    }

    private static string? ValidateQuestionRows(IReadOnlyList<QuestionBankRowDto>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return "请至少提交一道题目。";
        }

        string[] allowedTypes = ["选择题", "大题"];
        for (int i = 0; i < rows.Count; i++)
        {
            QuestionBankRowDto row = rows[i];
            int rowNumber = i + 1;
            if (string.IsNullOrWhiteSpace(row.Type) || string.IsNullOrWhiteSpace(row.Topic) || string.IsNullOrWhiteSpace(row.Direction) ||
                string.IsNullOrWhiteSpace(row.Scenario) || string.IsNullOrWhiteSpace(row.Difficulty) || string.IsNullOrWhiteSpace(row.Stem) ||
                string.IsNullOrWhiteSpace(row.Answer))
            {
                return $"第 {rowNumber} 道题存在空字段。";
            }

            if (!allowedTypes.Contains(row.Type.Trim()))
            {
                return $"第 {rowNumber} 道题题型必须是“选择题”或“大题”。";
            }

            if (row.Type.Trim() == "选择题" && string.IsNullOrWhiteSpace(row.OptionA) && string.IsNullOrWhiteSpace(row.OptionB) &&
                string.IsNullOrWhiteSpace(row.OptionC) && string.IsNullOrWhiteSpace(row.OptionD))
            {
                return $"第 {rowNumber} 道选择题至少需要一个选项。";
            }
        }

        return null;
    }
}
