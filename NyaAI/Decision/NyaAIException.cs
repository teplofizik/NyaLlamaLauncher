namespace NyaAI.Decision;

/// <summary>Ошибка при работе с нейронкой/сервером.</summary>
public sealed class NyaAIException : Exception
{
    public NyaAIException(string message) : base(message) { }
    public NyaAIException(string message, Exception inner) : base(message, inner) { }
}
