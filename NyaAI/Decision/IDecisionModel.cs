namespace NyaAI.Decision;

/// <summary>
/// Абстракция модели, принимающей решение по списку вариантов.
/// </summary>
/// <remarks>
/// Реализации:
/// <list type="bullet">
/// <item><description><c>NyaAI.Jev.JevStyleDecisionModel</c> — Jev-Style (через <c>/completion</c>).</description></item>
/// <item><description><c>NyaAI.Jev.JevOmniDecisionModel</c> — Jev-Omni (hidden + decision-head, медиа).</description></item>
/// <item><description><c>NyaAI.Generation.LlmDecisionModel</c> — любая обычная LLM как decision-модель.</description></item>
/// </list>
/// </remarks>
public interface IDecisionModel
{
    /// <summary>Принять решение по запросу.</summary>
    /// <param name="request">Состояние, вопрос и варианты.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат с вероятностями вариантов.</returns>
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Вспомогательные методы для <see cref="IDecisionModel"/>.</summary>
public static class DecisionModelExtensions
{
    /// <summary>Бинарное решение: true, если вероятность «да» ≥ <paramref name="threshold"/>.</summary>
    /// <param name="model">Decision-модель.</param>
    /// <param name="state">Контекст/состояние.</param>
    /// <param name="question">Вопрос.</param>
    /// <param name="threshold">Порог вероятности «да» (по умолчанию 0.5).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public static async Task<bool> IsYesAsync(
        this IDecisionModel model,
        string state,
        string question,
        double threshold = 0.5,
        CancellationToken cancellationToken = default)
    {
        var result = await model.DecideAsync(new DecisionRequest
        {
            State = state,
            Question = question,
            Options = new[] { "yes", "no" },
            Kind = DecisionKind.Bool
        }, cancellationToken);

        return (result.YesProbability ?? 0) >= threshold;
    }
}
