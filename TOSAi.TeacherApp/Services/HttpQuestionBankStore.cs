using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TOSAi.TeacherApp.Views;

namespace TOSAi.TeacherApp.Services;

public interface IQuestionBankStore
{
    string Description { get; }

    Task SaveAsync(IEnumerable<QuestionBankItem> rows, CancellationToken cancellationToken = default);

    Task<ObservableCollection<QuestionBankItem>> LoadAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class HttpQuestionBankStore : IQuestionBankStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpQuestionBankStore(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public string Description => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "云端 API";

    public async Task SaveAsync(IEnumerable<QuestionBankItem> rows, CancellationToken cancellationToken = default)
    {
        List<QuestionBankRowDto> payload = rows.Select(QuestionBankRowDto.FromQuestionBankItem).ToList();
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/questions/import-rows", payload, cancellationToken);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"云端题库保存失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }
    }

    public async Task<ObservableCollection<QuestionBankItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        QuestionBankRowsResponse? response = await _httpClient.GetFromJsonAsync<QuestionBankRowsResponse>("api/questions/import-rows", JsonOptions, cancellationToken);
        ObservableCollection<QuestionBankItem> rows = [];
        if (response?.Rows is null)
        {
            return rows;
        }

        foreach (QuestionBankRowDto row in response.Rows)
        {
            rows.Add(row.ToQuestionBankItem());
        }

        return rows;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return SaveAsync([], cancellationToken);
    }

    private sealed class QuestionBankRowsResponse
    {
        public List<QuestionBankRowDto>? Rows { get; set; }

        public int Count { get; set; }

        public string Storage { get; set; } = string.Empty;
    }

    private sealed class QuestionBankRowDto
    {
        public string Type { get; set; } = string.Empty;

        public string Topic { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public string Scenario { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public string Stem { get; set; } = string.Empty;

        public string OptionA { get; set; } = string.Empty;

        public string OptionB { get; set; } = string.Empty;

        public string OptionC { get; set; } = string.Empty;

        public string OptionD { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public static QuestionBankRowDto FromQuestionBankItem(QuestionBankItem item)
        {
            List<string> options = item.Options.Select(option => option.Text).Take(4).ToList();
            while (options.Count < 4)
            {
                options.Add(string.Empty);
            }

            return new QuestionBankRowDto
            {
                Type = item.Type,
                Topic = item.Topic,
                Direction = item.Direction,
                Scenario = item.Scenario,
                Difficulty = item.Difficulty,
                Stem = item.Stem,
                OptionA = options[0],
                OptionB = options[1],
                OptionC = options[2],
                OptionD = options[3],
                Answer = item.Answer,
                Explanation = item.Explanation
            };
        }

        public QuestionBankItem ToQuestionBankItem()
        {
            ObservableCollection<QuestionOption> options = [];
            foreach (string option in new[] { OptionA, OptionB, OptionC, OptionD })
            {
                if (!string.IsNullOrWhiteSpace(option))
                {
                    options.Add(new QuestionOption(option));
                }
            }

            return new QuestionBankItem(Type, Topic, Direction, Scenario, Difficulty, Stem, options, Answer, Explanation);
        }
    }
}
