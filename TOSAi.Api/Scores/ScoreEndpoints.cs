namespace TOSAi.Api;

internal static class ScoreEndpoints
{
    public static IEndpointRouteBuilder MapScoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scores/import-rows", async (HttpRequest request, IScoreImportRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            IReadOnlyList<ScoreImportRowDto> rows = await store.LoadAsync(cancellationToken);
            return Results.Ok(new ScoreImportRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapPost("/api/scores/import-rows", async (HttpRequest request, IReadOnlyList<ScoreImportRowDto> rows, IScoreImportRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            if (rows.Count > 0)
            {
                string? validationError = ValidateScoreRows(rows);
                if (validationError is not null)
                {
                    return Results.BadRequest(new { message = validationError });
                }
            }

            await store.SaveAsync(rows, cancellationToken);
            return Results.Ok(new ScoreImportRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapDelete("/api/scores/import-rows", async (HttpRequest request, IScoreImportRowStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            await store.ClearAsync(cancellationToken);
            return Results.Ok(new ScoreImportRowsResponse([], 0, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        return app;
    }

    private static string? ValidateScoreRows(IReadOnlyList<ScoreImportRowDto>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return "请至少提交一条成绩明细。";
        }

        for (int i = 0; i < rows.Count; i++)
        {
            ScoreImportRowDto row = rows[i];
            int rowNumber = i + 1;
            if (string.IsNullOrWhiteSpace(row.ExamName) || string.IsNullOrWhiteSpace(row.GradeName) || string.IsNullOrWhiteSpace(row.ClassName) ||
                string.IsNullOrWhiteSpace(row.StudentId) || string.IsNullOrWhiteSpace(row.StudentName) || string.IsNullOrWhiteSpace(row.SubjectName))
            {
                return $"第 {rowNumber} 条成绩明细存在空字段。";
            }

            if (row.Score < 0 || row.FullScore <= 0 || row.Score > row.FullScore)
            {
                return $"第 {rowNumber} 条成绩明细分数范围不正确。";
            }
        }

        return null;
    }
}
