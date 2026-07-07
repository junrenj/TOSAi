using Npgsql;

namespace TOSAi.Api;

interface IReportDraftStore
{
    Task<IReadOnlyList<ReportDraftDto>> LoadAsync(CancellationToken cancellationToken);

    Task<ReportDraftDto> SaveAsync(ReportDraftDto draft, CancellationToken cancellationToken);

    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

sealed class ReportDraftStore : IReportDraftStore
{
    private static readonly object MemoryLock = new();
    private static List<ReportDraftDto> memoryRows = [];
    private readonly NpgsqlDataSource? dataSource;

    public ReportDraftStore(NpgsqlDataSource? dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<ReportDraftDto>> LoadAsync(CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                return memoryRows.OrderByDescending(row => row.CreatedAt).ToList();
            }
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            select id, created_at, scope, prompt, summary, suggestions
            from report_drafts
            order by created_at desc;
            """);

        List<ReportDraftDto> rows = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ReportDraftDto(
                reader.GetString(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return rows;
    }

    public async Task<ReportDraftDto> SaveAsync(ReportDraftDto draft, CancellationToken cancellationToken)
    {
        ReportDraftDto normalized = Normalize(draft);
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                memoryRows.RemoveAll(row => row.Id == normalized.Id);
                memoryRows.Add(normalized);
            }

            return normalized;
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            insert into report_drafts (id, created_at, scope, prompt, summary, suggestions)
            values ($1, $2, $3, $4, $5, $6)
            on conflict (id) do update set
                created_at = excluded.created_at,
                scope = excluded.scope,
                prompt = excluded.prompt,
                summary = excluded.summary,
                suggestions = excluded.suggestions;
            """);
        command.Parameters.Add(new() { Value = normalized.Id });
        command.Parameters.Add(new() { Value = normalized.CreatedAt });
        command.Parameters.Add(new() { Value = normalized.Scope.Trim() });
        command.Parameters.Add(new() { Value = normalized.Prompt.Trim() });
        command.Parameters.Add(new() { Value = normalized.Summary.Trim() });
        command.Parameters.Add(new() { Value = normalized.Suggestions.Trim() });
        await command.ExecuteNonQueryAsync(cancellationToken);
        return normalized;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                memoryRows.RemoveAll(row => row.Id == id);
            }
            return;
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("delete from report_drafts where id = $1;");
        command.Parameters.Add(new() { Value = id });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ReportDraftDto Normalize(ReportDraftDto draft)
    {
        string id = string.IsNullOrWhiteSpace(draft.Id) ? Guid.NewGuid().ToString("N") : draft.Id.Trim();
        DateTimeOffset createdAt = draft.CreatedAt == default ? DateTimeOffset.UtcNow : draft.CreatedAt.ToUniversalTime();
        return draft with
        {
            Id = id,
            CreatedAt = createdAt,
            Scope = draft.Scope.Trim(),
            Prompt = draft.Prompt.Trim(),
            Summary = draft.Summary.Trim(),
            Suggestions = draft.Suggestions.Trim()
        };
    }

    private static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            create table if not exists report_drafts (
                id text primary key,
                created_at timestamptz not null,
                scope text not null,
                prompt text not null,
                summary text not null,
                suggestions text not null
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
