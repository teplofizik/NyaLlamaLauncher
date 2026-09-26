namespace NyaAI.Generation;

/// <summary>Параметры генерации. Незаданные (null) значения берутся из сервера.</summary>
public sealed class GenerationOptions
{
    /// <summary>Максимум новых токенов (n_predict / max_tokens).</summary>
    public int? MaxTokens { get; init; }

    /// <summary>Температура. null = серверное значение модели.</summary>
    public double? Temperature { get; init; }

    /// <summary>Nucleus sampling (top-p).</summary>
    public double? TopP { get; init; }

    /// <summary>Top-k sampling (0 = отключено).</summary>
    public int? TopK { get; init; }

    /// <summary>Min-p sampling.</summary>
    public double? MinP { get; init; }

    /// <summary>Стоп-последовательности.</summary>
    public IReadOnlyList<string>? Stop { get; init; }

    /// <summary>Зерно генерации.</summary>
    public uint? Seed { get; init; }

    /// <summary>Переиспользовать кэш промпта (ускоряет повторные вызовы).</summary>
    public bool CachePrompt { get; init; } = true;

    /// <summary>Пустые параметры по умолчанию.</summary>
    public static GenerationOptions Default { get; } = new();
}
