using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TOSAi.Api;

internal static class StoreServiceCollectionExtensions
{
    public static IServiceCollection AddTosAiStores(this IServiceCollection services)
    {
        string? databaseConnectionString = DatabaseConnectionOptions.ConnectionString;
        if (databaseConnectionString is not null)
        {
            string configuredConnectionString = databaseConnectionString;
            services.AddSingleton(_ => NpgsqlDataSource.Create(configuredConnectionString));
        }

        services.AddSingleton<IScoreImportRowStore>(provider => new ScoreImportRowStore(provider.GetService<NpgsqlDataSource>()));
        services.AddSingleton<IQuestionBankRowStore>(provider => new QuestionBankRowStore(provider.GetService<NpgsqlDataSource>()));
        services.AddSingleton<IQuestionDraftStore>(provider => new QuestionDraftStore(provider.GetService<NpgsqlDataSource>()));
        services.AddSingleton<IReportDraftStore>(provider => new ReportDraftStore(provider.GetService<NpgsqlDataSource>()));
        return services;
    }
}
