namespace TOSAi.TeacherApp.Services;

public sealed class MockAiAnalysisService : IAiAnalysisService
{
    public Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        AiAnalysisResult result = new(
            Summary: "当前为模拟分析结果：年级整体成绩稳定，数学和英语优势较明显，物理与化学存在部分风险学生。",
            Suggestions: "建议后续接入真实大模型后，将考试数据、年级均分、班级分布、学生历次变化作为上下文输入，输出分层教学建议和个体跟进清单。");

        return Task.FromResult(result);
    }
}
