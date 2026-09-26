using NyaAI.Generation;

namespace NyaAI.Llama;

public sealed class LlamaServerOptions
{
    /// <summary>Базовый адрес сервера, например http://127.0.0.1:8001.</summary>
    public string BaseUrl { get; init; } = "http://127.0.0.1:8001";

    /// <summary>API-ключ (заголовок Authorization: Bearer), если сервер его требует.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Имя модели для чат-запросов (alias). Может игнорироваться сервером.</summary>
    public string? Model { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Параметры генерации по умолчанию (если в вызове не заданы свои).</summary>
    public GenerationOptions DefaultGeneration { get; init; } = new();
}
