using Npgsql;

namespace TOSAi.Api;

interface IQuestionBankRowStore
{
    Task<IReadOnlyList<QuestionBankRowDto>> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(IReadOnlyList<QuestionBankRowDto> rows, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}

sealed class QuestionBankRowStore : IQuestionBankRowStore
{
    private static readonly object MemoryLock = new();
    private static List<QuestionBankRowDto> memoryRows = [];
    private readonly NpgsqlDataSource? dataSource;

    public QuestionBankRowStore(NpgsqlDataSource? dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<QuestionBankRowDto>> LoadAsync(CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                return memoryRows.ToList();
            }
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            select type, topic, direction, scenario, difficulty, stem, option_a, option_b, option_c, option_d, answer, explanation
            from question_bank_rows
            order by topic, difficulty, type, id;
            """);

        List<QuestionBankRowDto> rows = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new QuestionBankRowDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                reader.GetString(11)));
        }

        return rows;
    }

    public async Task SaveAsync(IReadOnlyList<QuestionBankRowDto> rows, CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                memoryRows = MergeRows(memoryRows, rows).ToList();
            }
            return;
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (QuestionBankRowDto row in rows)
        {
            await using (NpgsqlCommand deleteCommand = new("""
                delete from question_bank_rows
                where type = $1 and topic = $2 and direction = $3 and scenario = $4 and difficulty = $5 and stem = $6;
                """, connection, transaction))
            {
                deleteCommand.Parameters.Add(new() { Value = row.Type.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.Topic.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.Direction.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.Scenario.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.Difficulty.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.Stem.Trim() });
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using NpgsqlCommand insertCommand = new("""
                insert into question_bank_rows (type, topic, direction, scenario, difficulty, stem, option_a, option_b, option_c, option_d, answer, explanation)
                values ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12);
                """, connection, transaction)
            {
                Parameters =
                {
                    new() { Value = row.Type.Trim() },
                    new() { Value = row.Topic.Trim() },
                    new() { Value = row.Direction.Trim() },
                    new() { Value = row.Scenario.Trim() },
                    new() { Value = row.Difficulty.Trim() },
                    new() { Value = row.Stem.Trim() },
                    new() { Value = row.OptionA.Trim() },
                    new() { Value = row.OptionB.Trim() },
                    new() { Value = row.OptionC.Trim() },
                    new() { Value = row.OptionD.Trim() },
                    new() { Value = row.Answer.Trim() },
                    new() { Value = row.Explanation.Trim() }
                }
            };
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        if (this.dataSource is null)
        {
            lock (MemoryLock)
            {
                memoryRows.Clear();
            }
            return;
        }

        NpgsqlDataSource dataSource = this.dataSource;
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlCommand command = dataSource.CreateCommand("delete from question_bank_rows;");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<QuestionBankRowDto> MergeRows(IEnumerable<QuestionBankRowDto> existingRows, IEnumerable<QuestionBankRowDto> incomingRows)
    {
        Dictionary<string, QuestionBankRowDto> merged = existingRows.ToDictionary(BuildKey, StringComparer.OrdinalIgnoreCase);
        foreach (QuestionBankRowDto row in incomingRows)
        {
            merged[BuildKey(row)] = row;
        }

        return merged.Values
            .OrderBy(row => row.Topic)
            .ThenBy(row => row.Difficulty)
            .ThenBy(row => row.Type)
            .ThenBy(row => row.Stem)
            .ToList();
    }

    private static string BuildKey(QuestionBankRowDto row) => string.Join("|",
        row.Type.Trim(),
        row.Topic.Trim(),
        row.Direction.Trim(),
        row.Scenario.Trim(),
        row.Difficulty.Trim(),
        row.Stem.Trim());

    private static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            create table if not exists question_bank_rows (
                id bigserial primary key,
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
                explanation text not null default '',
                created_at timestamptz not null default now()
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
