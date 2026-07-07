namespace TOSAi.Api;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request) =>
        {
            string role = string.IsNullOrWhiteSpace(request.Role) ? "Teacher" : request.Role.Trim();
            string username = string.IsNullOrWhiteSpace(request.Username) ? role.ToLowerInvariant() : request.Username.Trim();
            string password = request.Password?.Trim() ?? string.Empty;

            DemoAccount? account = AuthState.FindAccount(username);
            if (account is null || !string.Equals(account.Password, password, StringComparison.Ordinal) || !string.Equals(account.Role, role, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Unauthorized();
            }

            CurrentUser user = new(account.Username, account.Role, account.DisplayName);
            string token = AuthState.IssueToken(user);
            return Results.Ok(new LoginResponse(token, user));
        });

        app.MapGet("/api/me", (HttpRequest request) =>
        {
            CurrentUser? user = AuthAuthorization.ReadCurrentUser(request);
            return user is null ? Results.Unauthorized() : Results.Ok(user);
        });

        return app;
    }
}
