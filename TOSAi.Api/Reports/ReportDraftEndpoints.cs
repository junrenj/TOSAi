namespace TOSAi.Api;

internal static class ReportDraftEndpoints
{
    public static IEndpointRouteBuilder MapReportDraftEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/drafts", async (HttpRequest request, IReportDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            IReadOnlyList<ReportDraftDto> rows = await store.LoadAsync(cancellationToken);
            return Results.Ok(new ReportDraftRowsResponse(rows, rows.Count, DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory"));
        });

        app.MapPost("/api/reports/drafts", async (HttpRequest request, ReportDraftDto draft, IReportDraftStore store, CancellationToken cancellationToken) =>
        {
            IResult? authorization = AuthAuthorization.RequireTeacher(request);
            if (authorization is not null)
            {
                return authorization;
            }

            string? validationError = ValidateReportDraft(draft);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            ReportDraftDto saved = await store.SaveAsync(draft, cancellationToken);
            return Results.Ok(saved);
        });

        app.MapDelete("/api/reports/drafts/{id}", async (HttpRequest request, string id, IReportDraftStore store, CancellationToken cancellationToken) =>
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

    private static string? ValidateReportDraft(ReportDraftDto? draft)
    {
        if (draft is null)
        {
            return "请提交报告草稿。";
        }

        if (string.IsNullOrWhiteSpace(draft.Scope) || string.IsNullOrWhiteSpace(draft.Prompt) ||
            string.IsNullOrWhiteSpace(draft.Summary) || string.IsNullOrWhiteSpace(draft.Suggestions))
        {
            return "报告草稿的范围、原始要求、摘要和教学建议不能为空。";
        }

        return null;
    }
}
