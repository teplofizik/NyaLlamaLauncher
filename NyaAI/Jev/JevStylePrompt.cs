using NyaAI.Decision;

namespace NyaAI.Jev;

/// <summary>Промпт decision-интерфейса Jev-Style (Qwen3.5-2B Decision).</summary>
public static class JevStylePrompt
{
    /// <summary>Собрать промпт решения из запроса.</summary>
    /// <param name="request">Запрос (используются State, Question, Options).</param>
    public static string Build(DecisionRequest request)
    {
        var lines = new List<string>
        {
            "You are a decision function. Read the state, then answer the question by choosing exactly one option.",
            "",
            "[State]",
            request.State,
            "",
            "[Question]",
            request.Question,
            "",
            "[Options]"
        };

        for (int i = 0; i < request.Options.Count; i++)
        {
            lines.Add($"{(char)('A' + i)}. {request.Options[i]}");
        }

        lines.Add("");
        lines.Add("Answer:");

        return string.Join("\n", lines);
    }
}
