namespace NyaAI.Generation;

/// <summary>Результат генерации текста.</summary>
public sealed class GenerationResult
{
    /// <summary>Сгенерированный текст.</summary>
    public string Text { get; init; } = "";

    /// <summary>Число токенов промпта.</summary>
    public int PromptTokens { get; init; }

    /// <summary>Число сгенерированных токенов.</summary>
    public int GeneratedTokens { get; init; }

    /// <summary>Причина остановки: "stop", "length", "eos" и т.п.</summary>
    public string StopReason { get; init; } = "";

    /// <summary>Скорость обработки промпта, токенов/с.</summary>
    public double PromptTokensPerSecond { get; init; }

    /// <summary>Скорость генерации, токенов/с.</summary>
    public double GenerationTokensPerSecond { get; init; }
}
