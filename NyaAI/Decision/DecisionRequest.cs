namespace NyaAI.Decision;

public enum DecisionKind
{
    /// <summary>Выбор одного из вариантов.</summary>
    Choice,

    /// <summary>Да/нет. Ожидается 2 варианта, «да» ищется по тексту (иначе первый).</summary>
    Bool,

    /// <summary>Порядковый балл: варианты от низшего к высшему.</summary>
    Score
}

/// <summary>Запрос на принятие решения (state + question + варианты [+ медиа]).</summary>
public sealed class DecisionRequest
{
    public string State { get; init; } = "";
    public string Question { get; init; } = "";

    /// <summary>Тексты вариантов (2..256). Для <see cref="DecisionKind.Score"/> — от низшего к высшему.</summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    public DecisionKind Kind { get; init; } = DecisionKind.Choice;

    /// <summary>Медиа для мультимодальных моделей (Jev-Omni). null/пусто — только текст.</summary>
    public IReadOnlyList<DecisionMedia>? Media { get; init; }
}
