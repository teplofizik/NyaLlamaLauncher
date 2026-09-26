using NyaAI.Decision;
using NyaAI.Generation;
using NyaAI.Llama;

namespace NyaAI.Examples;

/// <summary>Пример работы с обычной LLM: completion, чат, стриминг и decision.</summary>
public static class GenerationExample
{
    public static async Task RunAsync(ExampleConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.LlmModel))
        {
            Console.WriteLine("  LLM не задана (--llm-model=...), пропуск.");
            return;
        }

        var bootstrapArgs = new[]
        {
            "--model", cfg.LlmModel, "--alias", cfg.LlmAlias, "--jinja",
            "--host", "127.0.0.1", "--port", new Uri(cfg.BaseUrl).Port.ToString(),
            "--ctx-size", "8192", "--parallel", "1", "--flash-attn", "on", "-ngl", "99",
            "--api-key", cfg.ApiKey
        };
        using var server = await ServerBootstrap.EnsureAsync(cfg, bootstrapArgs);

        using var client = new LlamaServerClient(new LlamaServerOptions
        {
            BaseUrl = cfg.BaseUrl,
            ApiKey = cfg.ApiKey,
            Model = cfg.LlmAlias,
            DefaultGeneration = new GenerationOptions { MaxTokens = 64, Temperature = 0.2 }
        });
        var llm = new LlamaTextGenerator(client);

        var raw = await llm.GenerateAsync("The capital of France is",
            new GenerationOptions { MaxTokens = 16, Temperature = 0 });
        Console.WriteLine($"  [completion] {raw.Text.Trim()}");

        var chat = await llm.ChatAsync(new[]
        {
            ChatMessage.System("You are terse."),
            ChatMessage.User("In one short sentence: what is a mutex?")
        });
        Console.WriteLine($"  [chat] {chat.Text.Trim()}  ({chat.GenerationTokensPerSecond:0.0} t/s)");

        Console.Write("  [stream] ");
        await foreach (var tok in llm.ChatStreamAsync(new[] { ChatMessage.User("Count from 1 to 5, comma separated.") },
            new GenerationOptions { MaxTokens = 40, Temperature = 0 }))
        {
            Console.Write(tok);
        }
        Console.WriteLine();

        var decider = new LlmDecisionModel(llm, options: new GenerationOptions { MaxTokens = 16, Temperature = 0 });
        var d = await decider.DecideAsync(new DecisionRequest
        {
            State = "The film was excellent.",
            Question = "What is the sentiment?",
            Options = new[] { "negative", "positive" }
        });
        Console.WriteLine($"  [decision] best={d.Best.Option} p={d.Best.Probability:0.00}");
    }
}
