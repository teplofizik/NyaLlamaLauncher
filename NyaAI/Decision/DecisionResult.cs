namespace NyaAI.Decision;

/// <summary>Оценённый вариант решения.</summary>
public sealed record ScoredOption(int Index, string Option, double Probability, double LogProbability);

/// <summary>Результат решения.</summary>
public sealed class DecisionResult
{
    /// <summary>Варианты, отсортированные по убыванию вероятности.</summary>
    public IReadOnlyList<ScoredOption> Options { get; init; } = Array.Empty<ScoredOption>();

    /// <summary>Наиболее вероятный вариант.</summary>
    public ScoredOption Best => Options[0];

    /// <summary>Вероятность «да» для <see cref="DecisionKind.Bool"/>.</summary>
    public double? YesProbability { get; init; }

    /// <summary>Ожидаемый балл (zero-based) для <see cref="DecisionKind.Score"/>.</summary>
    public double? ExpectedScore { get; init; }

    /// <summary>Число токенов промпта (диагностика).</summary>
    public int PromptTokens { get; init; }

    /// <summary>Сырой ответ сервера (диагностика).</summary>
    public string RawContent { get; init; } = "";
}
