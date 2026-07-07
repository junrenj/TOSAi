namespace TOSAi.Api;

internal static class AuthAuthorization
{
    public static IResult? RequireTeacher(HttpRequest request)
    {
        return RequireRole(request, "Teacher");
    }

    public static IResult? RequireRole(HttpRequest request, string role)
    {
        CurrentUser? user = ReadCurrentUser(request);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase)
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public static CurrentUser? ReadCurrentUser(HttpRequest request)
    {
        string? authorization = request.Headers.Authorization.FirstOrDefault();
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = authorization["Bearer ".Length..].Trim();
        return AuthState.FindUser(token);
    }
}
