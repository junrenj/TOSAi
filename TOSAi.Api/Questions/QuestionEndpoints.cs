namespace TOSAi.Api;

internal static class QuestionEndpoints
{
    private static readonly string[] AllowedQuestionTypes = ["\u9009\u62e9\u9898", "\u5927\u9898"];
    private static readonly string[] AllowedDraftStatuses = ["\u5f85\u5ba1\u6838", "\u5df2\u786e\u8ba4", "\u5df2\u5e9f\u5f03"];

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

        app.MapGet("/api/questions/drafts", async (HttpRequest request, IQuestionDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            IReadOnlyList<QuestionDraftDto> rows = await store.LoadAsync(cancellationToken);
            return Results.Ok(new QuestionDraftRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapPost("/api/questions/drafts", async (HttpRequest request, QuestionDraftDto draft, IQuestionDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            string? validationError = ValidateQuestionDraft(draft);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            QuestionDraftDto saved = await store.SaveAsync(draft, cancellationToken);
            return Results.Ok(saved);
        });

        app.MapPatch("/api/questions/drafts/{id}/status", async (HttpRequest request, string id, QuestionDraftStatusUpdateRequest update, IQuestionDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            string? validationError = ValidateDraftStatus(update.Status);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            QuestionDraftDto? saved = await store.UpdateStatusAsync(id, update.Status, cancellationToken);
            return saved is null ? Results.NotFound() : Results.Ok(saved);
        });

        app.MapDelete("/api/questions/drafts/{id}", async (HttpRequest request, string id, IQuestionDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            await store.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private static string? ValidateQuestionRows(IReadOnlyList<QuestionBankRowDto>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return "Please submit at least one question.";
        }

        for (int i = 0; i < rows.Count; i++)
        {
            QuestionBankRowDto row = rows[i];
            int rowNumber = i + 1;
            if (string.IsNullOrWhiteSpace(row.Type) || string.IsNullOrWhiteSpace(row.Topic) || string.IsNullOrWhiteSpace(row.Direction) ||
                string.IsNullOrWhiteSpace(row.Scenario) || string.IsNullOrWhiteSpace(row.Difficulty) || string.IsNullOrWhiteSpace(row.Stem) ||
                string.IsNullOrWhiteSpace(row.Answer))
            {
                return $"Question {rowNumber} has required empty fields.";
            }

            if (!AllowedQuestionTypes.Contains(row.Type.Trim()))
            {
                return $"Question {rowNumber} type must be choice or essay.";
            }

            if (row.Type.Trim() == "\u9009\u62e9\u9898" && string.IsNullOrWhiteSpace(row.OptionA) && string.IsNullOrWhiteSpace(row.OptionB) &&
                string.IsNullOrWhiteSpace(row.OptionC) && string.IsNullOrWhiteSpace(row.OptionD))
            {
                return $"Choice question {rowNumber} needs at least one option.";
            }
        }

        return null;
    }

    private static string? ValidateQuestionDraft(QuestionDraftDto? draft)
    {
        if (draft is null)
        {
            return "Please submit a question draft.";
        }

        QuestionBankRowDto row = new(
            draft.Type,
            draft.Topic,
            draft.Direction,
            draft.Scenario,
            draft.Difficulty,
            draft.Stem,
            draft.OptionA,
            draft.OptionB,
            draft.OptionC,
            draft.OptionD,
            draft.Answer,
            string.Empty);

        string? questionError = ValidateQuestionRows([row]);
        return questionError ?? ValidateDraftStatus(draft.Status);
    }

    private static string? ValidateDraftStatus(string status)
    {
        if (!AllowedDraftStatuses.Contains(status.Trim()))
        {
            return "Question draft status must be pending, confirmed, or discarded.";
        }

        return null;
    }
}
