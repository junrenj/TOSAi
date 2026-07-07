using System.Net.Http;
using System.Net.Http.Headers;
using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public static class AuthSession
{
    public static string? Token { get; private set; }

    public static CurrentUser? User { get; private set; }

    public static bool IsSignedIn => !string.IsNullOrWhiteSpace(Token) && User is not null;

    public static void SignIn(string token, CurrentUser user)
    {
        Token = token;
        User = user;
    }

    public static void Apply(HttpClient httpClient)
    {
        if (!string.IsNullOrWhiteSpace(Token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }
    }
}