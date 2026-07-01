using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<IScoreImportRowStore, ScoreImportRowStore>();

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    name = "TOS AI Platform API",
    status = "running",
    storage = ScoreImportRowStore.HasConfiguredDatabase ? "postgres" : "memory",
    endpoints = new[]
    {
        "POST /api/auth/login",
        "GET /api/me",
        "GET /api/platform/{role}/{pageKey}",
        "GET /api/scores/import-rows",
        "POST /api/scores/import-rows"
    }
}));

app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    string role = string.IsNullOrWhiteSpace(request.Role) ? "Teacher" : request.Role.Trim();
    string username = string.IsNullOrWhiteSpace(request.Username) ? role.ToLowerInvariant() : request.Username.Trim();

    return Results.Ok(new LoginResponse(
        Token: $"demo-{role.ToLowerInvariant()}-token",
        User: new CurrentUser(username, role, GetDisplayName(role))));
});

app.MapGet("/api/me", (HttpRequest request) =>
{
    string role = ReadRoleFromBearerToken(request) ?? "Teacher";
    return Results.Ok(new CurrentUser($"demo-{role.ToLowerInvariant()}", role, GetDisplayName(role)));
});

app.MapGet("/api/platform/{role}/{pageKey}", (string role, string pageKey) =>
{
    PlatformFeaturePage page = CreatePage(role, pageKey);
    return Results.Ok(page);
});

app.MapGet("/api/scores/import-rows", async (IScoreImportRowStore store, CancellationToken cancellationToken) =>
{
    IReadOnlyList<ScoreImportRowDto> rows = await store.LoadAsync(cancellationToken);
    return Results.Ok(new ScoreImportRowsResponse(rows, rows.Count, ScoreImportRowStore.HasConfiguredDatabase ? "postgres" : "memory"));
});

app.MapPost("/api/scores/import-rows", async (IReadOnlyList<ScoreImportRowDto> rows, IScoreImportRowStore store, CancellationToken cancellationToken) =>
{
    if (rows.Count > 0)
    {
        string? validationError = ValidateScoreRows(rows);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }
    }

    await store.SaveAsync(rows, cancellationToken);
    return Results.Ok(new ScoreImportRowsResponse(rows, rows.Count, ScoreImportRowStore.HasConfiguredDatabase ? "postgres" : "memory"));
});

app.Run();

static string? ValidateScoreRows(IReadOnlyList<ScoreImportRowDto>? rows)
{
    if (rows is null || rows.Count == 0)
    {
        return "请至少提交一条成绩明细。";
    }

    for (int i = 0; i < rows.Count; i++)
    {
        ScoreImportRowDto row = rows[i];
        int rowNumber = i + 1;
        if (string.IsNullOrWhiteSpace(row.ExamName) || string.IsNullOrWhiteSpace(row.GradeName) || string.IsNullOrWhiteSpace(row.ClassName) ||
            string.IsNullOrWhiteSpace(row.StudentId) || string.IsNullOrWhiteSpace(row.StudentName) || string.IsNullOrWhiteSpace(row.SubjectName))
        {
            return $"第 {rowNumber} 条成绩明细存在空字段。";
        }

        if (row.Score < 0 || row.FullScore <= 0 || row.Score > row.FullScore)
        {
            return $"第 {rowNumber} 条成绩明细分数范围不正确。";
        }
    }

    return null;
}

static string? ReadRoleFromBearerToken(HttpRequest request)
{
    string? authorization = request.Headers.Authorization.FirstOrDefault();
    if (authorization is null || !authorization.StartsWith("Bearer demo-", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    string token = authorization["Bearer demo-".Length..];
    int suffixIndex = token.IndexOf("-token", StringComparison.OrdinalIgnoreCase);
    return suffixIndex > 0 ? token[..suffixIndex] : null;
}

static string GetDisplayName(string role) => role.ToLowerInvariant() switch
{
    "teacher" => "教师演示账号",
    "student" => "学生演示账号",
    "parent" => "家长演示账号",
    _ => "演示账号"
};

static PlatformFeaturePage CreatePage(string role, string pageKey)
{
    string normalizedRole = role.ToLowerInvariant();
    string normalizedPage = pageKey.Trim();

    if (normalizedRole == "teacher")
    {
        return normalizedPage switch
        {
            "teacherOverview" => Page(
                [Card("学生总数", "486", "覆盖初二年级 10 个班"), Card("平均分", "452.6", "较上次考试 +8.4"), Card("优势学生", "128", "至少 2 门学科显著领先"), Card("需关注", "37", "连续两次低于预警线")],
                [Activity("年级总览同步", "总览", "今天", "教师端总览数据已从云端返回。", "已更新"), Activity("重点学生名单", "提醒", "今天", "系统标记 37 名需关注学生。", "待查看")],
                "教师端总览来自服务器，展示当前授权范围内的整体数据。"),
            "teacherStudents" => Page(
                [Card("学生档案", "486", "已同步到云端"), Card("关注学生", "37", "待教师处理"), Card("班级数", "10", "当前学段"), Card("更新于", "刚刚", "服务器返回")],
                [Activity("初二 1 班名单", "档案", "今天", "已同步 48 名学生档案。", "已更新"), Activity("初二 2 班名单", "档案", "今天", "已同步 47 名学生档案。", "已更新")],
                "学生档案数据由服务器统一维护。"),
            "teacherScores" => Page(
                [Card("云端成绩", "可用", "支持保存和读取"), Card("数据库", ScoreImportRowStore.HasConfiguredDatabase ? "Postgres" : "内存", "由 API 自动选择"), Card("导入入口", "WPF", "成绩录入页面"), Card("趋势读取", "WPF", "学生趋势页面")],
                [Activity("成绩接口", "接口", "今天", "GET/POST /api/scores/import-rows 已启用。", "已上线"), Activity("数据闭环", "数据", "今天", "成绩录入和学生趋势可共用云端数据。", "可测试")],
                "成绩导入页面会把 CSV 明细保存到服务器；配置 DATABASE_URL 后会写入 PostgreSQL。"),
            "teacherTrends" => Page(
                [Card("考试次数", "6", "近三个月"), Card("平均得分率", "84%", "持续提升"), Card("优势学科", "数学", "稳定领先"), Card("薄弱学科", "英语", "阅读波动")],
                [Activity("学生趋势汇总", "趋势", "今天", "趋势页面数据来自云端成绩明细。", "已更新"), Activity("薄弱学科提醒", "提醒", "今天", "英语阅读波动学生已标记。", "待查看")],
                "学生趋势基于服务器中的成绩明细生成。"),
            "teacherAssignmentGenerator" => Page(
                [Card("已生成", "12", "本周"), Card("可替换题目", "48", "题库命中"), Card("导出作业", "7", "已发布"), Card("平均难度", "中等", "按班级适配")],
                [Activity("生物专题作业", "作业", "今天", "作业草稿已在云端生成。", "待确认"), Activity("题库匹配", "题库", "今天", "已优先匹配高相关题目。", "已完成")],
                "作业生成结果由服务器返回，前端只负责展示和编辑。"),
            "teacherAssignments" => Page(
                [Card("待布置任务", "18", "服务器返回的作业草稿"), Card("未完成学生", "34", "需教师跟进"), Card("批改队列", "52", "扫描录入与线上提交"), Card("本周完成率", "86%", "按班级汇总")],
                [Activity("初二 1 班数学分层练习", "作业", "今天", "针对函数综合题分层下发。", "待确认"), Activity("物理实验题专项", "练习", "本周", "面向连续两次实验题失分学生。", "草稿")],
                "教师端通过服务器获取授权班级和年级数据。"),
            "teacherReports" => Page(
                [Card("周报", "10", "按班级生成"), Card("家长报告", "486", "每名学生一份"), Card("风险提醒", "37", "待复核"), Card("已读反馈", "72%", "家长端阅读率")],
                [Activity("初二年级周学习报告", "报告", "今天", "汇总作业、考试趋势和学科优势。", "待发布"), Activity("家长沟通重点名单", "提醒", "本周", "连续两周进度下降学生。", "待处理")],
                "报告中心数据来自服务器，后续可保存发布状态和阅读状态。"),
            _ => Page(
                [Card("年级学生", "486", "来自云端"), Card("班级", "10", "教师授权范围"), Card("今日扫描", "128", "作业/考试记录"), Card("待关注", "37", "系统标记")],
                [Activity("扫描批次 20260624-A", "扫描", "今天", "已录入 128 条作业记录。", "已完成"), Activity("初二 1 班进度更新", "进度", "今天", "数学专项训练完成率 82%。", "已更新")],
                "教师端默认总览。")
        };
    }

    if (normalizedRole == "student")
    {
        return normalizedPage switch
        {
            "studentHome" => Page(
                [Card("今日任务", "5", "2 项已完成"), Card("学习进度", "68%", "本周计划"), Card("薄弱学科", "英语", "阅读理解"), Card("连续学习", "9 天", "保持节奏")],
                [Activity("今日学习概况", "任务", "今天", "学习首页数据已从服务器加载。", "已更新"), Activity("老师提醒", "提醒", "今天", "今日有 2 项任务需要完成。", "待查看")],
                "学生首页来自服务器，只展示本人数据。"),
            "studentHomework" => Page(
                [Card("待完成", "3", "今天 22:00 前"), Card("已提交", "12", "本周累计"), Card("待订正", "4", "错题需复盘"), Card("正确率", "84%", "较上周 +6%")],
                [Activity("数学函数基础练习", "作业", "今天", "共 10 题。", "待完成"), Activity("英语阅读理解 03", "作业", "今天", "完成后查看讲解。", "待完成")],
                "学生端只返回本人作业数据。"),
            "studentProgress" => Page(
                [Card("本周完成度", "68%", "计划执行正常"), Card("数学", "92%", "优势稳定"), Card("英语", "73%", "阅读波动"), Card("物理", "81%", "改善中")],
                [Activity("数学得分率提升", "趋势", "近 3 次", "函数题正确率从 76% 到 88%。", "提升"), Activity("学习计划调整", "计划", "本周", "英语阅读每天增加 10 分钟。", "已更新")],
                "学习计划和进度由服务器统一维护。"),
            "studentPhotoQuestion" => Page(
                [Card("拍照搜题", "待接入", "后续连接 OCR"), Card("讲解记录", "0", "占位"), Card("错题沉淀", "0", "进入错题本"), Card("相关练习", "0", "按题型推荐")],
                [Activity("上传题目照片", "入口", "随时", "后续接入图片上传和题目识别。", "占位")],
                "当前只保留拍照搜题入口，不实现 AI 解题逻辑。"),
            _ => Page(
                [Card("今日任务", "5", "2 项已完成"), Card("学习进度", "68%", "本周计划"), Card("薄弱学科", "英语", "阅读理解"), Card("连续学习", "9 天", "保持节奏")],
                [Activity("数学函数专项", "今日计划", "20 分钟", "完成 8 道基础题和 2 道提高题。", "进行中"), Activity("英语阅读训练", "今日计划", "15 分钟", "完成阅读并订正错题。", "未开始")],
                "学生首页来自服务器，只展示本人数据。")
        };
    }

    if (normalizedRole == "parent")
    {
        return normalizedPage switch
        {
            "parentHome" => Page(
                [Card("孩子排名", "25 / 486", "较上次 +8"), Card("作业完成", "92%", "本周完成率"), Card("优势学科", "数学", "稳定领先"), Card("需要关注", "英语", "阅读波动")],
                [Activity("本周学习概况", "报告", "今天", "学习节奏稳定，数学优势明显。", "已更新"), Activity("教师建议", "沟通", "本周", "每天固定 15 分钟阅读训练。", "建议")],
                "家长首页来自服务器，只展示绑定孩子的数据。"),
            "parentTrends" => Page(
                [Card("总分趋势", "上升", "连续 2 次提升"), Card("年级排名", "25", "较上次 +8"), Card("最高学科", "数学", "91%"), Card("最低学科", "英语", "76%")],
                [Activity("春季期中考试", "考试", "2026-04-20", "总分 451，年级排名 25。", "已完成"), Activity("三月月考", "考试", "2026-03-18", "总分 438，年级排名 33。", "已完成")],
                "家长端趋势只展示绑定孩子的数据。"),
            "parentReports" => Page(
                [Card("周报告", "1", "本周已生成"), Card("月报告", "1", "本月已生成"), Card("教师反馈", "3", "待阅读 1 条"), Card("家庭建议", "4", "可执行事项")],
                [Activity("第 12 周学习报告", "周报", "今天", "包含成绩趋势、作业完成和学习计划。", "新报告"), Activity("英语阅读专项反馈", "专项", "昨天", "建议家长关注阅读习惯。", "已读")],
                "报告正文后续可由模板或 AI 生成，但数据权限在服务器控制。"),
            "parentWellbeing" => Page(
                [Card("学习压力", "中等", "作业量增加"), Card("情绪状态", "稳定", "无明显异常"), Card("睡眠提醒", "需关注", "两天低于 7 小时"), Card("沟通建议", "2 条", "面向家长")],
                [Activity("压力波动提醒", "心理", "本周", "英语任务增加后压力偏高。", "关注"), Activity("家庭沟通建议", "建议", "今天", "先肯定执行情况，再讨论阅读训练。", "建议")],
                "心理情况只做学习支持提醒，不做医学诊断。"),
            _ => Page(
                [Card("孩子排名", "25 / 486", "较上次 +8"), Card("作业完成", "92%", "本周完成率"), Card("优势学科", "数学", "稳定领先"), Card("需要关注", "英语", "阅读波动")],
                [Activity("本周学习概况", "报告", "今天", "学习节奏稳定，数学优势明显。", "已更新"), Activity("教师建议", "沟通", "本周", "每天固定 15 分钟阅读训练。", "建议")],
                "家长首页来自服务器，只展示绑定孩子的数据。"),
        };
    }

    return Page([], [], "未知角色或页面。");
}

static PlatformFeaturePage Page(IReadOnlyList<PlatformMetricCard> cards, IReadOnlyList<PlatformActivityItem> activities, string note) => new(cards, activities, note);

static PlatformMetricCard Card(string label, string value, string hint) => new(label, value, hint);

static PlatformActivityItem Activity(string title, string category, string timeText, string detail, string status) => new(title, category, timeText, detail, status);

interface IScoreImportRowStore
{
    Task<IReadOnlyList<ScoreImportRowDto>> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(IReadOnlyList<ScoreImportRowDto> rows, CancellationToken cancellationToken);
}

sealed class ScoreImportRowStore : IScoreImportRowStore
{
    private static readonly object MemoryLock = new();
    private static List<ScoreImportRowDto> memoryRows = [];
    private readonly string? connectionString = NormalizeConnectionString(
        Environment.GetEnvironmentVariable("DATABASE_URL") ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));

    public static bool HasConfiguredDatabase => !string.IsNullOrWhiteSpace(NormalizeConnectionString(
        Environment.GetEnvironmentVariable("DATABASE_URL") ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")));

    public async Task<IReadOnlyList<ScoreImportRowDto>> LoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            lock (MemoryLock)
            {
                return memoryRows.ToList();
            }
        }

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);
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
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            lock (MemoryLock)
            {
                memoryRows = rows.ToList();
            }
            return;
        }

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);
        await EnsureSchemaAsync(dataSource, cancellationToken);
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (NpgsqlCommand deleteCommand = new("delete from score_import_rows;", connection, transaction))
        {
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (ScoreImportRowDto row in rows)
        {
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

    private static string? NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        string trimmed = connectionString.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
        {
            return trimmed;
        }

        if (uri.Scheme is not ("postgres" or "postgresql"))
        {
            return trimmed;
        }

        string[] userInfo = uri.UserInfo.Split(':', 2);
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require
        };

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            string query = uri.Query.TrimStart('?');
            foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);
                string key = Uri.UnescapeDataString(parts[0]);
                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<SslMode>(value, true, out SslMode sslMode))
                {
                    builder.SslMode = sslMode;
                }

            }
        }

        return builder.ConnectionString;
    }
}

sealed record LoginRequest(string Role, string Username, string Password);

sealed record LoginResponse(string Token, CurrentUser User);

sealed record CurrentUser(string Username, string Role, string DisplayName);

sealed record PlatformFeaturePage(IReadOnlyList<PlatformMetricCard> Cards, IReadOnlyList<PlatformActivityItem> Activities, string Note);

sealed record PlatformMetricCard(string Label, string Value, string Hint);

sealed record PlatformActivityItem(string Title, string Category, string TimeText, string Detail, string Status);

sealed record ScoreImportRowsResponse(IReadOnlyList<ScoreImportRowDto> Rows, int Count, string Storage);

sealed record ScoreImportRowDto(
    string ExamName,
    DateOnly ExamDate,
    string GradeName,
    string ClassName,
    string StudentId,
    string StudentName,
    string SubjectName,
    double Score,
    double FullScore)
{
    public double ScoreRate => FullScore <= 0 ? 0 : Math.Round(Score / FullScore * 100, 1);
}
