using NyaAI.Decision;
using NyaAI.Llama;

namespace NyaAI.Jev;

/// <summary>
/// Реализация <see cref="IDecisionModel"/> для Jev-Style decision-моделей:
/// один prefill возвращает логиты вариантов A..Z, нормируем их по объявленным
/// вариантам. Калибровка в calibrated-GGUF уже вшита, поэтому температура 1.0.
/// </summary>
public sealed class JevStyleDecisionModel : IDecisionModel
{
    private readonly LlamaServerClient _client;
    private readonly JevDecisionOptions _options;

    public JevStyleDecisionModel(LlamaServerClient client, JevDecisionOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? new JevDecisionOptions();
    }

    public async Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Media is { Count: > 0 })
            throw new NotSupportedException("Jev-Style не поддерживает медиа; используйте JevOmniDecisionModel.");

        var count = request.Options.Count;
        if (count is < 2 or > 26)
            throw new ArgumentException("Число вариантов должно быть от 2 до 26.", nameof(request));

        var prompt = JevStylePrompt.Build(request);
        var letters = new char[count];
        for (int i = 0; i < count; i++) letters[i] = (char)('A' + i);

        var nProbs = _options.InitialTopLogprobs;
        double[] weights;
        CompletionResult completion;

        while (true)
        {
            completion = await _client.CompleteAsync(prompt, nProbs, cancellationToken);

            weights = new double[count];
            var complete = true;
            for (int i = 0; i < count; i++)
            {
                weights[i] = SumLetterWeight(completion.TopLogProbs, letters[i]);
                if (weights[i] <= 0) complete = false;
            }

            if (complete || nProbs >= _options.MaxTopLogprobs) break;

            nProbs = (int)Math.Min(_options.MaxTopLogprobs, Math.Max(nProbs + 1, nProbs * _options.RetryGrowth));
        }

        var total = weights.Sum();
        if (total <= 0)
            throw new NyaAIException("Не удалось получить вероятности вариантов (ни один вариант не найден в top-N).");

        var scored = new List<ScoredOption>(count);
        for (int i = 0; i < count; i++)
        {
            var p = weights[i] / total;
            scored.Add(new ScoredOption(i, request.Options[i], p, Math.Log(Math.Max(p, 1e-30))));
        }

        var ranked = scored.OrderByDescending(s => s.Probability).ToList();

        double? yesProbability = null;
        double? expectedScore = null;

        if (request.Kind == DecisionKind.Bool)
        {
            var yesIndex = FindYesIndex(request.Options);
            yesProbability = weights[yesIndex] / total;
        }
        else if (request.Kind == DecisionKind.Score)
        {
            double expected = 0;
            for (int i = 0; i < count; i++) expected += i * (weights[i] / total);
            expectedScore = expected;
        }

        return new DecisionResult
        {
            Options = ranked,
            YesProbability = yesProbability,
            ExpectedScore = expectedScore,
            PromptTokens = completion.TokensEvaluated,
            RawContent = completion.Content
        };
    }

    private static double SumLetterWeight(IReadOnlyList<TokenLogProb> tokens, char letter)
    {
        double sum = 0;
        foreach (var t in tokens)
        {
            var s = t.Token.Trim();
            if (s.Length == 1 && s[0] == letter)
                sum += Math.Exp(t.LogProb);
        }
        return sum;
    }

    private static int FindYesIndex(IReadOnlyList<string> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i].Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0;
    }
}
