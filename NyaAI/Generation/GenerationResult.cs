namespace NyaAI.Generation;

public sealed class GenerationResult
{
    public string Text { get; init; } = "";
    public int PromptTokens { get; init; }
    public int GeneratedTokens { get; init; }

    /// <summary>Причина остановки: "stop", "length", "eos" и т.п.</summary>
    public string StopReason { get; init; } = "";

    public double PromptTokensPerSecond { get; init; }
    public double GenerationTokensPerSecond { get; init; }
}
