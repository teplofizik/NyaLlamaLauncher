namespace NyaAI.Generation;

/// <summary>
/// Абстракция текстового генератора (обычная LLM).
/// Реализации: llama.cpp (native /completion и OpenAI-совместимый /v1/chat/completions).
/// </summary>
public interface ITextGenerator
{
    // --- низкий уровень: чистый prompt ---

    Task<GenerationResult> GenerateAsync(
        string prompt,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // --- удобный уровень: чат с ролями ---

    Task<GenerationResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ChatStreamAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
