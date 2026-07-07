namespace TOSAi.Api;

sealed record LoginRequest(string Role, string Username, string Password);

sealed record LoginResponse(string Token, CurrentUser User);

sealed record CurrentUser(string Username, string Role, string DisplayName);

sealed record DemoAccount(string Username, string Password, string Role, string DisplayName);
