using TOSAi.TeacherApp.Models;

namespace TOSAi.TeacherApp.Services;

public sealed class MockPlatformApiClient : IPlatformApiClient
{
    public Task<PlatformFeaturePage> GetFeaturePageAsync(UserRole role, string pageKey, CancellationToken cancellationToken = default)
    {
        PlatformFeaturePage page = role switch
        {
            UserRole.Teacher => CreateTeacherPage(pageKey),
            UserRole.Student => CreateStudentPage(pageKey),
            UserRole.Parent => CreateParentPage(pageKey),
            _ => CreateEmptyPage()
        };

        return Task.FromResult(page);
    }

    private static PlatformFeaturePage CreateTeacherPage(string pageKey) => pageKey switch
    {
        "teacherAssignments" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("待布置任务", "18", "AI 建议与教师确认后下发"),
                Card("已下发作业", "126", "覆盖 10 个班级"),
                Card("未完成学生", "34", "需要班主任跟进"),
                Card("批改队列", "52", "来自扫描录入和线上提交")
            ],
            Activities =
            [
                Activity("初二 1 班数学分层练习", "作业", "今天", "针对函数综合题，分 A/B/C 三档。", "待确认"),
                Activity("初二 2 班英语词汇巩固", "作业", "明天", "低分段学生优先完成基础词汇训练。", "已下发"),
                Activity("物理实验探究题专项", "练习", "本周", "面向连续两次实验题失分学生。", "草稿")
            ],
            Note = "教师端拥有全年级数据权限，后续真实接口需要按学校、年级、班级、教师权限过滤。"
        },
        "teacherReports" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("周报", "10", "按班级生成"),
                Card("家长报告", "486", "每名学生一份"),
                Card("风险提醒", "37", "需人工复核"),
                Card("已读反馈", "72%", "家长端阅读率")
            ],
            Activities =
            [
                Activity("初二年级周学习报告", "报告", "今天 18:00", "汇总作业完成、考试趋势和学科优势。", "待发布"),
                Activity("李明个人学习报告", "报告", "昨天", "数学优势稳定，英语阅读需关注。", "已发布"),
                Activity("家长沟通重点名单", "提醒", "本周", "聚焦连续两周进度下降学生。", "待处理")
            ],
            Note = "报告中心先跑通列表和状态流转，报告正文后续可由 AI 或模板生成。"
        },
        _ => CreateEmptyPage()
    };

    private static PlatformFeaturePage CreateStudentPage(string pageKey) => pageKey switch
    {
        "studentHome" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("今日任务", "5", "2 项已完成"),
                Card("学习进度", "68%", "本周计划完成度"),
                Card("薄弱学科", "英语", "阅读理解需加强"),
                Card("连续学习", "9 天", "保持稳定节奏")
            ],
            Activities =
            [
                Activity("数学函数专项", "今日计划", "20 分钟", "完成 8 道基础题和 2 道提高题。", "进行中"),
                Activity("英语阅读训练", "今日计划", "15 分钟", "完成一篇阅读并订正错题。", "未开始"),
                Activity("物理实验题复盘", "复习", "10 分钟", "查看老师讲解并补充步骤表达。", "已完成")
            ],
            Note = "学生端只展示本人数据，目标是让学生知道今天该做什么、为什么做、做到什么程度。"
        },
        "studentHomework" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("待完成", "3", "今天 22:00 前"),
                Card("已提交", "12", "本周累计"),
                Card("待订正", "4", "错题需复盘"),
                Card("平均正确率", "84%", "较上周 +6%")
            ],
            Activities =
            [
                Activity("数学函数基础练习", "作业", "今天", "共 10 题，系统会记录错题。", "待完成"),
                Activity("英语阅读理解 03", "作业", "今天", "完成后查看逐题讲解。", "待完成"),
                Activity("化学方程式订正", "订正", "明天", "需重新提交错题过程。", "待订正")
            ],
            Note = "学生端作业流后续接入扫描录入、在线提交和错题讲解。"
        },
        "studentProgress" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("本周完成度", "68%", "计划执行正常"),
                Card("数学", "92%", "优势稳定"),
                Card("英语", "73%", "阅读波动"),
                Card("物理", "81%", "实验题改善中")
            ],
            Activities =
            [
                Activity("数学得分率提升", "趋势", "近 3 次", "函数题正确率从 76% 提升到 88%。", "提升"),
                Activity("英语阅读波动", "趋势", "近 3 次", "长难句理解失分较多。", "关注"),
                Activity("学习计划调整", "计划", "本周", "英语阅读每天增加 10 分钟。", "已更新")
            ],
            Note = "学习进度页会成为学生自己的学习计划入口。"
        },
        "studentPhotoQuestion" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("拍照搜题", "待接入", "后续连接 OCR 和题目解析"),
                Card("讲解记录", "0", "当前为框架占位"),
                Card("错题沉淀", "0", "将进入个人错题本"),
                Card("相关练习", "0", "按题型推荐")
            ],
            Activities =
            [
                Activity("上传题目照片", "入口", "随时", "后续接入 OCR、题目识别和分步讲解。", "占位"),
                Activity("查看讲解", "入口", "随时", "展示解题思路、关键知识点和同类题。", "占位")
            ],
            Note = "当前不实现 AI 解题逻辑，只保留学生端拍照搜题和题目讲解的业务入口。"
        },
        _ => CreateEmptyPage()
    };

    private static PlatformFeaturePage CreateParentPage(string pageKey) => pageKey switch
    {
        "parentHome" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("孩子排名", "25 / 486", "较上次 +8"),
                Card("作业完成", "92%", "本周完成率"),
                Card("优势学科", "数学", "稳定领先"),
                Card("需要关注", "英语", "阅读理解波动")
            ],
            Activities =
            [
                Activity("本周学习概况", "报告", "今天", "学习节奏稳定，数学优势明显。", "已更新"),
                Activity("作业完成提醒", "作业", "昨天", "英语阅读训练有一次延迟提交。", "已查看"),
                Activity("教师建议", "沟通", "本周", "每天固定 15 分钟阅读训练。", "建议")
            ],
            Note = "家长端只展示孩子本人数据，重点是解释清楚趋势、风险和家庭配合建议。"
        },
        "parentTrends" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("总分趋势", "上升", "连续 2 次提升"),
                Card("年级排名", "25", "较上次 +8"),
                Card("最高学科", "数学", "得分率 91%"),
                Card("最低学科", "英语", "得分率 76%")
            ],
            Activities =
            [
                Activity("春季期中考试", "考试", "2026-04-20", "总分 451，年级排名 25。", "已完成"),
                Activity("三月月考", "考试", "2026-03-18", "总分 438，年级排名 33。", "已完成"),
                Activity("二月摸底", "考试", "2026-02-25", "总分 421，年级排名 48。", "已完成")
            ],
            Note = "家长端趋势页后续应避免过度排名焦虑，突出可行动建议。"
        },
        "parentReports" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("周报告", "1", "本周已生成"),
                Card("月报告", "1", "本月已生成"),
                Card("教师反馈", "3", "待阅读 1 条"),
                Card("家庭建议", "4", "可执行事项")
            ],
            Activities =
            [
                Activity("第 12 周学习报告", "周报", "今天", "包含成绩趋势、作业完成和学习计划。", "新报告"),
                Activity("英语阅读专项反馈", "专项", "昨天", "建议家长关注阅读习惯。", "已读"),
                Activity("月度成长报告", "月报", "本月", "整体进步明显，心理状态稳定。", "已读")
            ],
            Note = "学习报告页先跑通报告列表和阅读状态，报告正文后续接模板或 AI 生成。"
        },
        "parentWellbeing" => new PlatformFeaturePage
        {
            Cards =
            [
                Card("学习压力", "中等", "近期作业量增加"),
                Card("情绪状态", "稳定", "无明显异常"),
                Card("睡眠提醒", "需关注", "两天低于 7 小时"),
                Card("沟通建议", "2 条", "面向家长")
            ],
            Activities =
            [
                Activity("压力波动提醒", "心理", "本周", "英语任务增加后反馈压力偏高。", "关注"),
                Activity("家庭沟通建议", "建议", "今天", "先肯定执行情况，再讨论阅读训练。", "建议"),
                Activity("睡眠作息提醒", "习惯", "昨天", "建议 22:30 前完成学习收尾。", "提醒")
            ],
            Note = "心理情况页只做趋势和提醒，不做医学诊断。正式版本需要谨慎设计权限、措辞和告警机制。"
        },
        _ => CreateEmptyPage()
    };

    private static PlatformFeaturePage CreateEmptyPage() => new()
    {
        Cards = [],
        Activities = [],
        Note = "该页面还没有配置模拟数据。"
    };

    private static PlatformMetricCard Card(string label, string value, string hint) => new()
    {
        Label = label,
        Value = value,
        Hint = hint
    };

    private static PlatformActivityItem Activity(string title, string category, string timeText, string detail, string status) => new()
    {
        Title = title,
        Category = category,
        TimeText = timeText,
        Detail = detail,
        Status = status
    };
}
