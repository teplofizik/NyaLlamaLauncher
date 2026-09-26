namespace NyaAI.Decision;

/// <summary>
/// Decision-модель из лямбды — удобно для тестов и простых правил.
/// </summary>
/// <example>
/// <code>
/// IDecisionModel model = new DelegatingDecisionModel((req, ct) =>
///     Task.FromResult(new DecisionResult
///     {
///         Options = new[] { new ScoredOption(0, req.Options[0], 1.0, 0.0) }
///     }));
/// </code>
/// </example>
public sealed class DelegatingDecisionModel : IDecisionModel
{
    private readonly Func<DecisionRequest, CancellationToken, Task<DecisionResult>> _impl;

    /// <summary>Создать модель с указанной функцией.</summary>
    /// <param name="impl">Функция, реализующая решение.</param>
    public DelegatingDecisionModel(Func<DecisionRequest, CancellationToken, Task<DecisionResult>> impl) =>
        _impl = impl ?? throw new ArgumentNullException(nameof(impl));

    /// <inheritdoc />
    public Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default) =>
        _impl(request, cancellationToken);
}
