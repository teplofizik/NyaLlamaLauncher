namespace NyaAI.Decision;

/// <summary>Позволяет задать decision-модель лямбдой.</summary>
public sealed class DelegatingDecisionModel : IDecisionModel
{
    private readonly Func<DecisionRequest, CancellationToken, Task<DecisionResult>> _impl;

    public DelegatingDecisionModel(Func<DecisionRequest, CancellationToken, Task<DecisionResult>> impl) =>
        _impl = impl ?? throw new ArgumentNullException(nameof(impl));

    public Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default) =>
        _impl(request, cancellationToken);
}
