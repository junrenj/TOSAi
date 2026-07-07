using TOSAi.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddTosAiStores();

var app = builder.Build();

app.UseCors();

app.MapRootEndpoint();
app.MapAuthEndpoints();
app.MapPlatformEndpoints();
app.MapScoreEndpoints();
app.MapQuestionEndpoints();
app.MapReportDraftEndpoints();

app.Run();
