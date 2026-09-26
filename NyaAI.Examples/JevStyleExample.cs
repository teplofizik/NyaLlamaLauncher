using NyaAI.Decision;
using NyaAI.Jev;
using NyaAI.Llama;

namespace NyaAI.Examples;

/// <summary>Пример Jev-Style: выбор / да-нет / балл.</summary>
public static class JevStyleExample
{
    public static async Task RunAsync(ExampleConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.JevStyleModel))
        {
            Console.WriteLine("  Jev-Style модель не задана (--jevstyle-model=...), пропуск.");
            return;
        }

        var bootstrapArgs = new[]
        {
            "--model", cfg.JevStyleModel, "--alias", "jev-style",
            "--host", "127.0.0.1", "--port", new Uri(cfg.BaseUrl).Port.ToString(),
            "--ctx-size", "2048", "--parallel", "1", "--flash-attn", "on", "-ngl", "99", "--no-warmup",
            "--api-key", cfg.ApiKey
        };
        using var server = await ServerBootstrap.EnsureAsync(cfg, bootstrapArgs);

        using var client = new LlamaServerClient(new LlamaServerOptions { BaseUrl = cfg.BaseUrl, ApiKey = cfg.ApiKey });
        var model = new JevStyleDecisionModel(client);

        var choice = await model.DecideAsync(new DecisionRequest
        {
            State = "The film was excellent.",
            Question = "What is the sentiment of this review?",
            Options = new[] { "negative", "positive" }
        });
        Console.WriteLine($"  [choice] {choice.Best.Option} p={choice.Best.Probability:0.0000}");

        var yes = await model.IsYesAsync(
            "The meeting starts at 10 AM. It is now 9 AM.", "Has the meeting started?");
        Console.WriteLine($"  [bool]   started = {yes} (ожидается False)");

        var score = await model.DecideAsync(new DecisionRequest
        {
            State = "I absolutely loved it, one of the best films I have seen.",
            Question = "Rate the sentiment on a 0-4 scale.",
            Options = new[] { "very negative", "negative", "neutral", "positive", "very positive" },
            Kind = DecisionKind.Score
        });
        Console.WriteLine($"  [score]  expected={score.ExpectedScore:0.00} best={score.Best.Option}");
    }
}
