namespace NyaAI.Generation;

/// <summary>Стандартные роли сообщений.</summary>
public static class ChatRoles
{
    /// <summary>Системная роль.</summary>
    public const string System = "system";

    /// <summary>Роль пользователя.</summary>
    public const string User = "user";

    /// <summary>Роль ассистента.</summary>
    public const string Assistant = "assistant";
}

/// <summary>Сообщение чата (роль + содержимое).</summary>
/// <param name="Role">Роль: см. <see cref="ChatRoles"/>.</param>
/// <param name="Content">Текст сообщения.</param>
public sealed record ChatMessage(string Role, string Content)
{
    /// <summary>Системное сообщение.</summary>
    public static ChatMessage System(string c) => new(ChatRoles.System, c);

    /// <summary>Сообщение пользователя.</summary>
    public static ChatMessage User(string c) => new(ChatRoles.User, c);

    /// <summary>Сообщение ассистента.</summary>
    public static ChatMessage Assistant(string c) => new(ChatRoles.Assistant, c);
}
