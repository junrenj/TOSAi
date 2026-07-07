namespace TOSAi.TeacherApp.Models;

public sealed record CurrentUser(string Username, string Role, string DisplayName);

public sealed record LoginResponse(string Token, CurrentUser User);