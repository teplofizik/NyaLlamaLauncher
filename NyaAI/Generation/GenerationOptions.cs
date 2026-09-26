namespace NyaAI.Generation;

public sealed class GenerationOptions
{
    public int? MaxTokens { get; init; }

    /// <summary>Температура. null = серверное значение модели.</summary>
    public double? Temperature { get; init; }

    public double? TopP { get; init; }
    public int? TopK { get; init; }
    public double? MinP { get; init; }

    public IReadOnlyList<string>? Stop { get; init; }

    public uint? Seed { get; init; }

    /// <summary>Кэш промпта (для переиспользования префикса).</summary>
    public bool CachePrompt { get; init; } = true;

    public static GenerationOptions Default { get; } = new();
}
