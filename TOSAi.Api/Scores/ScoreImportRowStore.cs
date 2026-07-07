using Npgsql;

namespace TOSAi.Api;

interface IScoreImportRowStore
{
    Task<IReadOnlyList<ScoreImportRowDto>> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(IReadOnlyList<ScoreImportRowDto> rows, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}

sealed class ScoreImportRowStore : IScoreImportRowStore
{
    private static readonly object MemoryLock = new();
    private static List<ScoreImportRowDto> memoryRows = [];
    private readonly NpgsqlDataSource? dataSource;

    public ScoreImportRowStore(NpgsqlDataSource? dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<ScoreImportRowDto>> LoadAsync(CancellationToken cancellationToken)
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
            select exam_name, exam_date, grade_name, class_name, student_id, student_name, subject_name, score, full_score
            from score_import_rows
            order by exam_date, exam_name, class_name, student_name, subject_name;
            """);

        List<ScoreImportRowDto> rows = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ScoreImportRowDto(
                reader.GetString(0),
                DateOnly.FromDateTime(reader.GetDateTime(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetDouble(7),
                reader.GetDouble(8)));
        }

        return rows;
    }

    public async Task SaveAsync(IReadOnlyList<ScoreImportRowDto> rows, CancellationToken cancellationToken)
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

        foreach (ScoreImportRowDto row in rows)
        {
            await using (NpgsqlCommand deleteCommand = new("""
                delete from score_import_rows
                where exam_name = $1 and exam_date = $2 and student_id = $3 and subject_name = $4;
                """, connection, transaction))
            {
                deleteCommand.Parameters.Add(new() { Value = row.ExamName.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.ExamDate.ToDateTime(TimeOnly.MinValue) });
                deleteCommand.Parameters.Add(new() { Value = row.StudentId.Trim() });
                deleteCommand.Parameters.Add(new() { Value = row.SubjectName.Trim() });
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using NpgsqlCommand insertCommand = new("""
                insert into score_import_rows (exam_name, exam_date, grade_name, class_name, student_id, student_name, subject_name, score, full_score)
                values ($1, $2, $3, $4, $5, $6, $7, $8, $9);
                """, connection, transaction)
            {
                Parameters =
                {
                    new() { Value = row.ExamName.Trim() },
                    new() { Value = row.ExamDate.ToDateTime(TimeOnly.MinValue) },
                    new() { Value = row.GradeName.Trim() },
                    new() { Value = row.ClassName.Trim() },
                    new() { Value = row.StudentId.Trim() },
                    new() { Value = row.StudentName.Trim() },
                    new() { Value = row.SubjectName.Trim() },
                    new() { Value = row.Score },
                    new() { Value = row.FullScore }
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
        await using NpgsqlCommand command = dataSource.CreateCommand("delete from score_import_rows;");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<ScoreImportRowDto> MergeRows(IEnumerable<ScoreImportRowDto> existingRows, IEnumerable<ScoreImportRowDto> incomingRows)
    {
        Dictionary<string, ScoreImportRowDto> merged = existingRows.ToDictionary(BuildKey, StringComparer.OrdinalIgnoreCase);
        foreach (ScoreImportRowDto row in incomingRows)
        {
            merged[BuildKey(row)] = row;
        }

        return merged.Values
            .OrderBy(row => row.ExamDate)
            .ThenBy(row => row.ExamName)
            .ThenBy(row => row.ClassName)
            .ThenBy(row => row.StudentName)
            .ThenBy(row => row.SubjectName)
            .ToList();
    }

    private static string BuildKey(ScoreImportRowDto row) => string.Join("|",
        row.ExamName.Trim(),
        row.ExamDate.ToString("yyyy-MM-dd"),
        row.StudentId.Trim(),
        row.SubjectName.Trim());

    private static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand("""
            create table if not exists score_import_rows (
                id bigserial primary key,
                exam_name text not null,
                exam_date date not null,
                grade_name text not null,
                class_name text not null,
                student_id text not null,
                student_name text not null,
                subject_name text not null,
                score double precision not null,
                full_score double precision not null,
                created_at timestamptz not null default now()
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
