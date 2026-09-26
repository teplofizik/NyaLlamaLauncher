using System.Text.Json;

namespace NyaAI.Llama;

/// <summary>Токен и его лог-вероятность на очередной позиции.</summary>
/// <param name="Token">Текст токена.</param>
/// <param name="LogProb">Натуральный логарифм вероятности.</param>
public sealed record TokenLogProb(string Token, double LogProb);

/// <summary>Результат вызова native <c>/completion</c> у llama.cpp.</summary>
public sealed class CompletionResult
{
    /// <summary>Сгенерированный текст.</summary>
    public string Content { get; init; } = "";

    /// <summary>Число токенов промпта.</summary>
    public int TokensEvaluated { get; init; }

    /// <summary>Скорость обработки промпта, токенов/с.</summary>
    public double PromptPerSecond { get; init; }

    /// <summary>Распределение следующего токена (top-N), если запрошено <c>n_probs</c>.</summary>
    public IReadOnlyList<TokenLogProb> TopLogProbs { get; init; } = Array.Empty<TokenLogProb>();

    internal static CompletionResult Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        var evaluated = root.TryGetProperty("tokens_evaluated", out var te) && te.TryGetInt32(out var tev) ? tev : 0;

        double pps = 0;
        if (root.TryGetProperty("timings", out var timings) &&
            timings.TryGetProperty("prompt_per_second", out var ppsEl) &&
            ppsEl.TryGetDouble(out var ppsv))
        {
            pps = ppsv;
        }

        var probs = new List<TokenLogProb>();

        if (root.TryGetProperty("completion_probabilities", out var cp) &&
            cp.ValueKind == JsonValueKind.Array && cp.GetArrayLength() > 0)
        {
            var first = cp[0];

            // Новый формат: top_logprobs: [{ token, logprob }]
            if (first.TryGetProperty("top_logprobs", out var top) && top.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in top.EnumerateArray())
                {
                    var token = ReadString(item, "token") ?? ReadString(item, "tok_str") ?? "";
                    var logProb = ReadDouble(item, "logprob") ?? LogOf(ReadDouble(item, "prob"));
                    probs.Add(new TokenLogProb(token, logProb));
                }
            }
            // Старый формат: probs: [{ tok_str, prob }]
            else if (first.TryGetProperty("probs", out var old) && old.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in old.EnumerateArray())
                {
                    var token = ReadString(item, "tok_str") ?? ReadString(item, "token") ?? "";
                    var logProb = ReadDouble(item, "logprob") ?? LogOf(ReadDouble(item, "prob"));
                    probs.Add(new TokenLogProb(token, logProb));
                }
            }
        }

        return new CompletionResult
        {
            Content = content,
            TokensEvaluated = evaluated,
            PromptPerSecond = pps,
            TopLogProbs = probs
        };
    }

    private static string? ReadString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? ReadDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.TryGetDouble(out var d) ? d : null;

    private static double LogOf(double? prob) =>
        Math.Log(Math.Max(prob ?? 0, 1e-30));
}
