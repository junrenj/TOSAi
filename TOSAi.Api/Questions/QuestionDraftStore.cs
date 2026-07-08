using Npgsql;

namespace TOSAi.Api;

interface IQuestionDraftStore
{
    Task<IReadOnlyList<QuestionDraftDto>> LoadAsync(CancellationToken cancellationToken);

    Task<QuestionDraftDto> SaveAsync(QuestionDraftDto draft, CancellationToken cancellationToken);

    Task<QuestionDraftDto?> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken);

    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

sealed class QuestionDraftStore : IQuestionDraftStore
{
    private static readonly object MemoryLock = new();
    private static List<QuestionDraftDto> memoryRows = [];
    private readonly NpgsqlDataSource? dataSource;

    public QuestionDraftStore(NpgsqlDataSource? dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<QuestionDraftDto>> LoadAsync(CancellationToken cancellationToken)
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
            select id, created_at, status, type, topic, direction, scenario, difficulty, stem,
                   option_a, option_b, option_c, option_d, answer, source_prompt, reference_count
            from question_drafts
            order by created_at desc;
            """);

        List<QuestionDraftDto> rows = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(ReadDraft(reader));
        }

        return rows;
    }

    public async Task<QuestionDraftDto> SaveAsync(QuestionDraftDto draft, CancellationToken cancellationToken)
    {
        QuestionDraftDto normalized = Normalize(draft);
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
            insert into question_drafts (
                id, created_at, status, type, topic, direction, scenario, difficulty, stem,
                option_a, option_b, option_c, option_d, answer, source_prompt, reference_count)
            values ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16)
            on conflict (id) do update set
                created_at = excluded.created_at,
                status = excluded.status,
                type = excluded.type,
                topic = excluded.topic,
                direction = excluded.direction,
                scenario = excluded.scenario,
                difficulty = excluded.difficulty,
                stem = excluded.stem,
                option_a = excluded.option_a,
                option_b = excluded.option_b,
                option_c = excluded.option_c,
                option_d = excluded.option_d,
                answer = excluded.answer,
                source_prompt = excluded.source_prompt,
                reference_count = excluded.reference_count;
            """);
        AddParameters(command, normalized);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return normalized;
    }

    public async Task<QuestionDraftDto?> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken)
    {
        string normalizedId = id.Trim();
        string normalizedStatus = NormalizeStatus(status);

        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                int index = memoryRows.FindIndex(row => row.Id == normalizedId);
                if (index < 0)
                {
                    return null;
                }

                QuestionDraftDto updated = memoryRows[index] with { Status = normalizedStatus };
                memoryRows[index] = updated;
                return updated;
            }
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            update question_drafts set status = $1 where id = $2
            returning id, created_at, status, type, topic, direction, scenario, difficulty, stem,
                      option_a, option_b, option_c, option_d, answer, source_prompt, reference_count;
            """);
        command.Parameters.Add(new() { Value = normalizedStatus });
        command.Parameters.Add(new() { Value = normalizedId });

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDraft(reader) : null;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                memoryRows.RemoveAll(row => row.Id == id.Trim());
            }
            return;
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("delete from question_drafts where id = $1;");
        command.Parameters.Add(new() { Value = id.Trim() });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static QuestionDraftDto ReadDraft(NpgsqlDataReader reader) => new(
        reader.GetString(0),
        reader.GetFieldValue<DateTimeOffset>(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetString(7),
        reader.GetString(8),
        reader.GetString(9),
        reader.GetString(10),
        reader.GetString(11),
        reader.GetString(12),
        reader.GetString(13),
        reader.GetString(14),
        reader.GetInt32(15));

    private static QuestionDraftDto Normalize(QuestionDraftDto draft)
    {
        string id = string.IsNullOrWhiteSpace(draft.Id) ? Guid.NewGuid().ToString("N") : draft.Id.Trim();
        DateTimeOffset createdAt = draft.CreatedAt == default ? DateTimeOffset.UtcNow : draft.CreatedAt.ToUniversalTime();

        return draft with
        {
            Id = id,
            CreatedAt = createdAt,
            Status = NormalizeStatus(draft.Status),
            Type = draft.Type.Trim(),
            Topic = draft.Topic.Trim(),
            Direction = draft.Direction.Trim(),
            Scenario = draft.Scenario.Trim(),
            Difficulty = draft.Difficulty.Trim(),
            Stem = draft.Stem.Trim(),
            OptionA = draft.OptionA.Trim(),
            OptionB = draft.OptionB.Trim(),
            OptionC = draft.OptionC.Trim(),
            OptionD = draft.OptionD.Trim(),
            Answer = draft.Answer.Trim(),
            SourcePrompt = draft.SourcePrompt.Trim(),
            ReferenceCount = Math.Max(0, draft.ReferenceCount)
        };
    }

    private static string NormalizeStatus(string status) => string.IsNullOrWhiteSpace(status) ? "\u5f85\u5ba1\u6838" : status.Trim();

    private static void AddParameters(NpgsqlCommand command, QuestionDraftDto draft)
    {
        command.Parameters.Add(new() { Value = draft.Id });
        command.Parameters.Add(new() { Value = draft.CreatedAt });
        command.Parameters.Add(new() { Value = draft.Status });
        command.Parameters.Add(new() { Value = draft.Type });
        command.Parameters.Add(new() { Value = draft.Topic });
        command.Parameters.Add(new() { Value = draft.Direction });
        command.Parameters.Add(new() { Value = draft.Scenario });
        command.Parameters.Add(new() { Value = draft.Difficulty });
        command.Parameters.Add(new() { Value = draft.Stem });
        command.Parameters.Add(new() { Value = draft.OptionA });
        command.Parameters.Add(new() { Value = draft.OptionB });
        command.Parameters.Add(new() { Value = draft.OptionC });
        command.Parameters.Add(new() { Value = draft.OptionD });
        command.Parameters.Add(new() { Value = draft.Answer });
        command.Parameters.Add(new() { Value = draft.SourcePrompt });
        command.Parameters.Add(new() { Value = draft.ReferenceCount });
    }

    private static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            create table if not exists question_drafts (
                id text primary key,
                created_at timestamptz not null,
                status text not null,
                type text not null,
                topic text not null,
                direction text not null,
                scenario text not null,
                difficulty text not null,
                stem text not null,
                option_a text not null default '',
                option_b text not null default '',
                option_c text not null default '',
                option_d text not null default '',
                answer text not null,
                source_prompt text not null default '',
                reference_count integer not null default 0
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
