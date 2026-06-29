var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    name = "TOS AI Platform API",
    status = "running",
    endpoints = new[]
    {
        "POST /api/auth/login",
        "GET /api/me",
        "GET /api/platform/{role}/{pageKey}"
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

app.Run();

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
                "家长首页来自服务器，只展示绑定孩子的数据。")
        };
    }

    return Page([], [], "未知角色或页面。 ");
}

static PlatformFeaturePage Page(IReadOnlyList<PlatformMetricCard> cards, IReadOnlyList<PlatformActivityItem> activities, string note) => new(cards, activities, note);

static PlatformMetricCard Card(string label, string value, string hint) => new(label, value, hint);

static PlatformActivityItem Activity(string title, string category, string timeText, string detail, string status) => new(title, category, timeText, detail, status);

sealed record LoginRequest(string Role, string Username, string Password);

sealed record LoginResponse(string Token, CurrentUser User);

sealed record CurrentUser(string Username, string Role, string DisplayName);

sealed record PlatformFeaturePage(IReadOnlyList<PlatformMetricCard> Cards, IReadOnlyList<PlatformActivityItem> Activities, string Note);

sealed record PlatformMetricCard(string Label, string Value, string Hint);

sealed record PlatformActivityItem(string Title, string Category, string TimeText, string Detail, string Status);
