namespace TOSAi.Api;

internal static class RootEndpoints
{
    public static IEndpointRouteBuilder MapRootEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new
        {
            name = "TOS AI Platform API",
            status = "running",
            storage = DatabaseConnectionOptions.HasConfiguredDatabase ? "postgres" : "memory",
            endpoints = new[]
            {
                "POST /api/auth/login",
                "GET /api/me",
                "GET /api/platform/{role}/{pageKey}",
                "GET /api/scores/import-rows",
                "POST /api/scores/import-rows",
                "DELETE /api/scores/import-rows",
                "GET /api/questions/import-rows",
                "POST /api/questions/import-rows",
                "DELETE /api/questions/import-rows",
                "GET    /api/reports/drafts",
                "POST   /api/reports/drafts",
                "DELETE /api/reports/drafts/{id}"
            }
        }));

        return app;
    }
}
