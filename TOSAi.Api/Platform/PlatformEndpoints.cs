namespace TOSAi.Api;

internal static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/platform/{role}/{pageKey}", (HttpRequest request, string role, string pageKey) =>
        {
            IResult? authorization = AuthAuthorization.RequireRole(request, role);
            if (authorization is not null)
            {
                return authorization;
            }

            PlatformFeaturePage page = PlatformPageFactory.CreatePage(role, pageKey);
            return Results.Ok(page);
        });

        return app;
    }
}
