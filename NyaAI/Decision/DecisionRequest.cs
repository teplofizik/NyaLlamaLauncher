namespace NyaAI.Decision;

/// <summary>Тип решения, который должна принять модель.</summary>
public enum DecisionKind
{
    /// <summary>Выбор одного варианта из списка.</summary>
    Choice,

    /// <summary>Да/нет. Ожидается два варианта; «да» ищется по тексту (иначе первый).</summary>
    Bool,

    /// <summary>Порядковый балл: варианты перечисляются от низшего к высшему.</summary>
    Score
}

/// <summary>
/// Запрос на принятие решения: состояние, вопрос и варианты ответа
/// (для мультимодальных моделей — ещё и медиа).
/// </summary>
/// <example>
/// <code>
/// var request = new DecisionRequest
/// {
///     State = "Товар доставлен, но упаковка повреждена.",
///     Question = "Одобрить возврат?",
///     Options = new[] { "yes", "no" },
///     Kind = DecisionKind.Bool
/// };
/// var result = await model.DecideAsync(request);
/// </code>
/// </example>
public sealed class DecisionRequest
{
    /// <summary>Контекст/состояние (например, описание ситуации или записи).</summary>
    public string State { get; init; } = "";

    /// <summary>Вопрос, на который модель должна ответить выбором варианта.</summary>
    public string Question { get; init; } = "";

    /// <summary>
    /// Тексты вариантов (2..256). Для <see cref="DecisionKind.Score"/> — от низшего к высшему.
    /// </summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    /// <summary>Тип решения. По умолчанию <see cref="DecisionKind.Choice"/>.</summary>
    public DecisionKind Kind { get; init; } = DecisionKind.Choice;

    /// <summary>Медиа для мультимодальных моделей (Jev-Omni). null/пусто — только текст.</summary>
    public IReadOnlyList<DecisionMedia>? Media { get; init; }
}
