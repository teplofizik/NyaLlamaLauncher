using NyaAI.Decision;

namespace NyaAI.Generation;

/// <summary>
/// Использование обычной LLM как decision-модели: промпт строится стратегией
/// <see cref="ILlmDecisionPrompt"/>, ответ разбирается обратно в <see cref="DecisionResult"/>.
/// </summary>
/// <example>
/// <code>
/// var decider = new LlmDecisionModel(textGenerator);
/// var r = await decider.DecideAsync(new DecisionRequest
/// {
///     State = "The film was excellent.",
///     Question = "Sentiment?",
///     Options = new[] { "negative", "positive" }
/// });
/// Console.WriteLine($"{r.Best.Option} {r.Best.Probability:0.00}");
/// </code>
/// </example>
public sealed class LlmDecisionModel : IDecisionModel
{
    private readonly ITextGenerator _generator;
    private readonly ILlmDecisionPrompt _prompt;
    private readonly GenerationOptions? _options;

    /// <summary>Создать decision-модель поверх текстового генератора.</summary>
    /// <param name="generator">Генератор (обычная LLM).</param>
    /// <param name="prompt">Стратегия промпта/разбора (null = <see cref="LlmOptionPrompt"/>).</param>
    /// <param name="options">Параметры генерации (обычно достаточно <c>MaxTokens = 16</c>).</param>
    public LlmDecisionModel(
        ITextGenerator generator,
        ILlmDecisionPrompt? prompt = null,
        GenerationOptions? options = null)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _prompt = prompt ?? new LlmOptionPrompt();
        _options = options;
    }

    /// <inheritdoc />
    public async Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            ChatMessage.System(_prompt.System),
            ChatMessage.User(_prompt.BuildUser(request))
        };

        var result = await _generator.ChatAsync(messages, _options, cancellationToken);
        var decision = _prompt.Parse(result.Text, request);

        return new DecisionResult
        {
            Options = decision.Options,
            YesProbability = decision.YesProbability,
            ExpectedScore = decision.ExpectedScore,
            PromptTokens = result.PromptTokens,
            RawContent = result.Text
        };
    }
}
