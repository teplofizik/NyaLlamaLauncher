namespace NyaAI.Generation;

/// <summary>
/// Абстракция текстового генератора (обычная LLM).
/// Реализация: <c>NyaAI.Llama.LlamaTextGenerator</c>.
/// </summary>
/// <remarks>
/// Два уровня: низкий — <see cref="GenerateAsync"/> (чистый промпт, native
/// <c>/completion</c>) и удобный — <see cref="ChatAsync"/> (роли,
/// <c>/v1/chat/completions</c>). Для обоих есть потоковые версии.
/// </remarks>
public interface ITextGenerator
{
    /// <summary>Сгенерировать текст по промпту.</summary>
    /// <param name="prompt">Текст промпта.</param>
    /// <param name="options">Параметры генерации (null = по умолчанию).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<GenerationResult> GenerateAsync(
        string prompt,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Потоково сгенерировать текст по промпту (по мере появления токенов).</summary>
    /// <param name="prompt">Текст промпта.</param>
    /// <param name="options">Параметры генерации (null = по умолчанию).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Чат-генерация по списку сообщений (с ролями).</summary>
    /// <param name="messages">Сообщения чата.</param>
    /// <param name="options">Параметры генерации (null = по умолчанию).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<GenerationResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Потоковая чат-генерация по списку сообщений.</summary>
    /// <param name="messages">Сообщения чата.</param>
    /// <param name="options">Параметры генерации (null = по умолчанию).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    IAsyncEnumerable<string> ChatStreamAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
