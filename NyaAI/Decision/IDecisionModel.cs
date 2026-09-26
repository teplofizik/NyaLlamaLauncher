namespace NyaAI.Decision;

/// <summary>
/// Абстракция модели, принимающей решение по списку вариантов.
/// Реализации: Jev-Style (llama.cpp), в перспективе Jev-Omni, openjev и др.
/// </summary>
public interface IDecisionModel
{
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}

public static class DecisionModelExtensions
{
    /// <summary>Бинарное решение: true, если вероятность «да» ≥ <paramref name="threshold"/>.</summary>
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
