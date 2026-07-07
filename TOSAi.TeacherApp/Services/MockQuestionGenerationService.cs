namespace TOSAi.TeacherApp.Services;

public sealed class MockQuestionGenerationService : IQuestionGenerationService
{
    public Task<IReadOnlyList<GeneratedQuestionDraft>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken = default)
    {
        List<GeneratedQuestionDraft> drafts = [];
        int variant = 1;

        for (int i = 0; i < request.ChoiceCount; i++)
        {
            QuestionGenerationReference? reference = PickReference(request, "选择题", i);
            drafts.Add(CreateChoiceDraft(request, reference, variant++));
        }

        for (int i = 0; i < request.EssayCount; i++)
        {
            QuestionGenerationReference? reference = PickReference(request, "大题", i + request.ChoiceCount);
            drafts.Add(CreateEssayDraft(request, reference, variant++));
        }

        return Task.FromResult<IReadOnlyList<GeneratedQuestionDraft>>(drafts);
    }

    private static QuestionGenerationReference? PickReference(QuestionGenerationRequest request, string type, int index)
    {
        List<QuestionGenerationReference> candidates = request.References
            .Where(item => item.Type == type)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = request.References.ToList();
        }

        return candidates.Count == 0 ? null : candidates[index % candidates.Count];
    }

    private static GeneratedQuestionDraft CreateChoiceDraft(QuestionGenerationRequest request, QuestionGenerationReference? reference, int variant)
    {
        string topic = reference?.Topic ?? Pick(request.Topics, variant);
        string direction = reference?.Direction ?? Pick(request.Directions, variant);
        string scenario = reference?.Scenario ?? Pick(request.Scenarios, variant);
        string difficulty = request.Difficulty;
        string stem = $"AI 草稿 {variant}：在“{scenario}”情境下，围绕“{topic}”设计一个变式问题。下列哪项最能体现“{direction}”的思维要求？";
        string answer = reference is null
            ? "答案：B。解析：应结合情境信息、核心概念和推理方向进行判断。"
            : $"答案：B。解析：本题参考云端题库中的“{reference.Topic}”题目进行变式，重点考察{direction}。";

        return new GeneratedQuestionDraft(
            "选择题",
            difficulty,
            topic,
            direction,
            scenario,
            stem,
            [
                "A. 只复述材料表面信息，缺少概念解释。",
                $"B. 提取关键变量，并用“{topic}”相关概念解释现象。",
                "C. 忽略题干条件，直接套用固定结论。",
                "D. 只讨论社会影响，不分析生物学机制。"
            ],
            answer);
    }

    private static GeneratedQuestionDraft CreateEssayDraft(QuestionGenerationRequest request, QuestionGenerationReference? reference, int variant)
    {
        string topic = reference?.Topic ?? Pick(request.Topics, variant);
        string direction = reference?.Direction ?? Pick(request.Directions, variant);
        string scenario = reference?.Scenario ?? Pick(request.Scenarios, variant);
        string difficulty = request.Difficulty;
        string stem = $"AI 草稿 {variant}：请基于“{scenario}”情境，围绕“{topic}”完成分析：1. 提取关键信息；2. 说明相关机制；3. 按“{direction}”要求完成推理；4. 写出一个限制条件。";
        string answer = reference is null
            ? "参考答案：应包含情境证据、核心概念、推理链条和限制条件。"
            : $"参考答案：本题参考云端题库中的“{reference.Topic}”题目进行变式，应体现证据提取、机制解释和{direction}。";

        return new GeneratedQuestionDraft("大题", difficulty, topic, direction, scenario, stem, [], answer);
    }

    private static string Pick(IReadOnlyList<string> values, int index) => values.Count == 0 ? string.Empty : values[index % values.Count];
}