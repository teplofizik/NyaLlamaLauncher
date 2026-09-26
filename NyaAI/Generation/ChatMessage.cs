namespace NyaAI.Generation;

public static class ChatRoles
{
    public const string System = "system";
    public const string User = "user";
    public const string Assistant = "assistant";
}

public sealed record ChatMessage(string Role, string Content)
{
    public static ChatMessage System(string c) => new(ChatRoles.System, c);
    public static ChatMessage User(string c) => new(ChatRoles.User, c);
    public static ChatMessage Assistant(string c) => new(ChatRoles.Assistant, c);
}
