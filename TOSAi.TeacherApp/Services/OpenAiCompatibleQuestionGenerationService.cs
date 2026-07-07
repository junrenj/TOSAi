using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TOSAi.TeacherApp.Services;

public sealed class OpenAiCompatibleQuestionGenerationService : IQuestionGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AiSettings _settings;
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleQuestionGenerationService(AiSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<IReadOnlyList<GeneratedQuestionDraft>> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (_settings.UseMockAnalysis)
        {
            throw new InvalidOperationException("AI 接口尚未配置完整，请先在系统设置中填写接口地址、模型名称和 API Key。");
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, BuildChatCompletionsUri(_settings.Endpoint));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(new ChatCompletionRequest(
            _settings.Model,
            [
                new ChatMessage("system", "你是一名严谨的中学生物命题老师。你必须基于参考题生成新题，保持知识点一致，但不能简单复制题干或选项。只返回 JSON，不要返回 Markdown。"),
                new ChatMessage("user", BuildPrompt(request))
            ],
            0.35));

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI 题目生成失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        ChatCompletionResponse? completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText, JsonOptions);
        string content = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI 接口没有返回可用题目。");
        }

        QuestionGenerationResponse? generated = JsonSerializer.Deserialize<QuestionGenerationResponse>(ExtractJson(content), JsonOptions);
        List<GeneratedQuestionDraft> drafts = generated?.Questions?.Select(ToDraft).ToList() ?? [];
        ValidateDrafts(drafts, request.ChoiceCount + request.EssayCount);
        return drafts;
    }

    private static string BuildPrompt(QuestionGenerationRequest request)
    {
        StringBuilder builder = new();
        builder.AppendLine("请基于以下组卷需求和参考题，生成新的题目草稿。");
        builder.AppendLine();
        builder.AppendLine("组卷需求：");
        builder.AppendLine($"- 难度：{request.Difficulty}");
        builder.AppendLine($"- 主题：{string.Join("、", request.Topics)}");
        builder.AppendLine($"- 考察方向：{string.Join("、", request.Directions)}");
        builder.AppendLine($"- 情景分类：{string.Join("、", request.Scenarios)}");
        builder.AppendLine($"- 选择题数量：{request.ChoiceCount}");
        builder.AppendLine($"- 大题数量：{request.EssayCount}");
        builder.AppendLine();
        builder.AppendLine("参考题：");

        int index = 1;
        foreach (QuestionGenerationReference reference in request.References.Take(8))
        {
            builder.AppendLine($"参考题 {index++}：");
            builder.AppendLine($"题型：{reference.Type}");
            builder.AppendLine($"主题：{reference.Topic}");
            builder.AppendLine($"考察方向：{reference.Direction}");
            builder.AppendLine($"情景分类：{reference.Scenario}");
            builder.AppendLine($"难度：{reference.Difficulty}");
            builder.AppendLine($"题干：{reference.Stem}");
            if (reference.Options.Count > 0)
            {
                builder.AppendLine($"选项：{string.Join("；", reference.Options)}");
            }
            builder.AppendLine($"答案：{reference.Answer}");
            builder.AppendLine($"解析：{reference.Explanation}");
            builder.AppendLine();
        }

        builder.AppendLine("输出要求：");
        builder.AppendLine("1. 必须生成全新的题干，不能复制参考题原文。");
        builder.AppendLine("2. 选择题至少 4 个选项，答案和解析写在 answer 字段中。");
        builder.AppendLine("3. 大题 options 返回空数组。");
        builder.AppendLine("4. 只返回下面结构的 JSON，不要包含 Markdown 代码块。");
        builder.AppendLine("{");
        builder.AppendLine("  \"questions\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"type\": \"选择题\",");
        builder.AppendLine("      \"difficulty\": \"应用推理\",");
        builder.AppendLine("      \"topic\": \"孟德尔遗传\",");
        builder.AppendLine("      \"direction\": \"演绎和推理\",");
        builder.AppendLine("      \"scenario\": \"家系分析\",");
        builder.AppendLine("      \"stem\": \"...\",");
        builder.AppendLine("      \"options\": [\"A. ...\", \"B. ...\", \"C. ...\", \"D. ...\"],");
        builder.AppendLine("      \"answer\": \"答案：B。解析：...\"");
        builder.AppendLine("    }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string ExtractJson(string content)
    {
        string trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            int firstLineEnd = trimmed.IndexOf('\n');
            int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
            {
                trimmed = trimmed[(firstLineEnd + 1)..lastFence].Trim();
            }
        }

        int start = trimmed.IndexOf('{');
        int end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new JsonException("AI 返回内容不是有效 JSON。\n" + content);
        }

        return trimmed[start..(end + 1)];
    }

    private static GeneratedQuestionDraft ToDraft(GeneratedQuestionDto dto)
    {
        return new GeneratedQuestionDraft(
            dto.Type.Trim(),
            dto.Difficulty.Trim(),
            dto.Topic.Trim(),
            dto.Direction.Trim(),
            dto.Scenario.Trim(),
            dto.Stem.Trim(),
            dto.Options?.Where(option => !string.IsNullOrWhiteSpace(option)).Select(option => option.Trim()).ToList() ?? [],
            dto.Answer.Trim());
    }

    private static void ValidateDrafts(IReadOnlyList<GeneratedQuestionDraft> drafts, int expectedCount)
    {
        if (drafts.Count == 0)
        {
            throw new InvalidOperationException("AI 没有生成任何题目。");
        }

        if (drafts.Count != expectedCount)
        {
            throw new InvalidOperationException($"AI 返回 {drafts.Count} 道题，但本次请求需要 {expectedCount} 道题。");
        }

        for (int i = 0; i < drafts.Count; i++)
        {
            GeneratedQuestionDraft draft = drafts[i];
            int rowNumber = i + 1;
            if (draft.Type is not ("选择题" or "大题"))
            {
                throw new InvalidOperationException($"第 {rowNumber} 道题题型必须是选择题或大题。");
            }

            if (string.IsNullOrWhiteSpace(draft.Stem) || string.IsNullOrWhiteSpace(draft.Answer) ||
                string.IsNullOrWhiteSpace(draft.Topic) || string.IsNullOrWhiteSpace(draft.Difficulty))
            {
                throw new InvalidOperationException($"第 {rowNumber} 道题缺少题干、答案、主题或难度。");
            }

            if (draft.Type == "选择题" && draft.Options.Count < 2)
            {
                throw new InvalidOperationException($"第 {rowNumber} 道选择题至少需要 2 个选项。");
            }
        }
    }

    private static string BuildChatCompletionsUri(string endpoint)
    {
        string normalizedEndpoint = endpoint.Trim().TrimEnd('/');
        return normalizedEndpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? normalizedEndpoint
            : $"{normalizedEndpoint}/chat/completions";
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] List<ChatChoice>? Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message);

    private sealed class QuestionGenerationResponse
    {
        public List<GeneratedQuestionDto>? Questions { get; set; }
    }

    private sealed class GeneratedQuestionDto
    {
        public string Type { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Scenario { get; set; } = string.Empty;
        public string Stem { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public string Answer { get; set; } = string.Empty;
    }
}