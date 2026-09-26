namespace NyaAI.Decision;

/// <summary>Оценённый вариант решения.</summary>
/// <param name="Index">Позиция варианта в исходном запросе (0-based).</param>
/// <param name="Option">Текст варианта.</param>
/// <param name="Probability">Нормированная вероятность варианта.</param>
/// <param name="LogProbability">Натуральный логарифм вероятности.</param>
public sealed record ScoredOption(int Index, string Option, double Probability, double LogProbability);

/// <summary>Результат решения.</summary>
public sealed class DecisionResult
{
    /// <summary>Варианты, отсортированные по убыванию вероятности.</summary>
    public IReadOnlyList<ScoredOption> Options { get; init; } = Array.Empty<ScoredOption>();

    /// <summary>Наиболее вероятный вариант (первый элементов <see cref="Options"/>).</summary>
    public ScoredOption Best => Options[0];

    /// <summary>Вероятность «да» для <see cref="DecisionKind.Bool"/>, иначе null.</summary>
    public double? YesProbability { get; init; }

    /// <summary>Ожидаемый балл (zero-based) для <see cref="DecisionKind.Score"/>, иначе null.</summary>
    public double? ExpectedScore { get; init; }

    /// <summary>Число токенов промпта (диагностика).</summary>
    public int PromptTokens { get; init; }

    /// <summary>Сырой ответ/диагностика сервера.</summary>
    public string RawContent { get; init; } = "";
}
