namespace NyaAI.Decision;

/// <summary>Ошибка при работе с нейронкой/сервером (HTTP, парсинг, ffmpeg и т.п.).</summary>
public sealed class NyaAIException : Exception
{
    /// <summary>Создать исключение с сообщением.</summary>
    public NyaAIException(string message) : base(message) { }

    /// <summary>Создать исключение с сообщением и вложенной ошибкой.</summary>
    public NyaAIException(string message, Exception inner) : base(message, inner) { }
}
