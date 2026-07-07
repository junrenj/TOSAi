using System.Security.Cryptography;

namespace TOSAi.Api;

static class AuthState
{
    private static readonly object TokenLock = new();
    private static readonly Dictionary<string, DemoAccount> Accounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["teacher"] = new("teacher", "teacher123", "Teacher", "教师演示账号"),
        ["student"] = new("student", "student123", "Student", "学生演示账号"),
        ["parent"] = new("parent", "parent123", "Parent", "家长演示账号")
    };
    private static readonly Dictionary<string, CurrentUser> IssuedTokens = new(StringComparer.Ordinal);

    public static DemoAccount? FindAccount(string username) => Accounts.TryGetValue(username, out DemoAccount? account) ? account : null;

    public static string IssueToken(CurrentUser user)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        lock (TokenLock)
        {
            IssuedTokens[token] = user;
        }

        return token;
    }

    public static CurrentUser? FindUser(string token)
    {
        lock (TokenLock)
        {
            return IssuedTokens.TryGetValue(token, out CurrentUser? user) ? user : null;
        }
    }
}
