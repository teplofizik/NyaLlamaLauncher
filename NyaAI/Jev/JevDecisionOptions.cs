namespace NyaAI.Jev;

public sealed class JevDecisionOptions
{
    /// <summary>Сколько top-логвероятностей запрашивать изначально.</summary>
    public int InitialTopLogprobs { get; init; } = 100;

    /// <summary>Верхний предел при доборе вариантов, не попавших в top-N.</summary>
    public int MaxTopLogprobs { get; init; } = 8000;

    /// <summary>Во сколько раз увеличивать N при повторе.</summary>
    public double RetryGrowth { get; init; } = 4.0;
}
