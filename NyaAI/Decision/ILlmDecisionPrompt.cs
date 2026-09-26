namespace NyaAI.Decision;

/// <summary>
/// Стратегия превращения <see cref="DecisionRequest"/> в промпт для обычной LLM
/// и разбора её ответа обратно в <see cref="DecisionResult"/>.
/// Реализация по умолчанию — <c>NyaAI.Generation.LlmOptionPrompt</c>.
/// </summary>
public interface ILlmDecisionPrompt
{
    /// <summary>Системная инструкция.</summary>
    string System { get; }

    /// <summary>Пользовательская часть промпта.</summary>
    string BuildUser(DecisionRequest request);

    /// <summary>Разобрать текстовый ответ модели.</summary>
    DecisionResult Parse(string output, DecisionRequest request);
}
