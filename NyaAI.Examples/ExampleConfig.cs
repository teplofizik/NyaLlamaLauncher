namespace NyaAI.Examples;

/// <summary>
/// Настройки для примеров. Значения можно переопределять аргументами командной
/// строки или переменными окружения (NYAAI_*), не трогая код.
/// </summary>
public sealed class ExampleConfig
{
    public string BaseUrl { get; init; } = Env("NYAAI_BASE_URL", "http://127.0.0.1:8001");
    public string ApiKey { get; init; } = Env("NYAAI_API_KEY",
        "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ=");

    /// <summary>GGUF обычной LLM (для генерации/LLM-decision). Пусто = не запускать эти примеры.</summary>
    public string? LlmModel { get; init; } = EnvOpt("NYAAI_LLM_MODEL");

    /// <summary>Alias обычной LLM.</summary>
    public string LlmAlias { get; init; } = Env("NYAAI_LLM_ALIAS", "local/model");

    /// <summary>GGUF Jev-Style (decision через /completion). Пусто = пропустить.</summary>
    public string? JevStyleModel { get; init; } = EnvOpt("NYAAI_JEVSTYLE_MODEL");

    /// <summary>GGUF Jev-Omni (decision + медиа). Пусто = пропустить.</summary>
    public string? JevOmniModel { get; init; } = EnvOpt("NYAAI_JEVOMNI_MODEL");

    /// <summary>Файл decision-head-f32.npz для Jev-Omni.</summary>
    public string? JevOmniHead { get; init; } = EnvOpt("NYAAI_JEVOMNI_HEAD");

    /// <summary>mmproj для Jev-Omni (для медиа). Пусто — пример запустит только текст.</summary>
    public string? JevOmniMmproj { get; init; } = EnvOpt("NYAAI_JEVOMNI_MMPROJ");

    public string? FfmpegPath { get; init; } = EnvOpt("NYAAI_FFMPEG");

    /// <summary>Если задан — примеры сами стартуют llama-server этим exe.</summary>
    public string? ServerExe { get; init; } = EnvOpt("NYAAI_SERVER_EXE");

    public static string AssetsDir =>
        Path.Combine(AppContext.BaseDirectory, "Assets");

    private static string Env(string name, string? fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : (fallback ?? "");

    private static string? EnvOpt(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : null;

    /// <summary>Разбор простых --key=value аргументов.</summary>
    public static ExampleConfig FromArgs(string[] args)
    {
        var b = new ExampleConfig();
        string? GetOpt(string key, string? current) =>
            args.FirstOrDefault(a => a.StartsWith($"--{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1] ?? current;
        string Get(string key, string current) => GetOpt(key, current) ?? current;

        return new ExampleConfig
        {
            BaseUrl = Get("base-url", b.BaseUrl),
            ApiKey = Get("api-key", b.ApiKey),
            LlmModel = GetOpt("llm-model", b.LlmModel),
            LlmAlias = Get("llm-alias", b.LlmAlias),
            JevStyleModel = GetOpt("jevstyle-model", b.JevStyleModel),
            JevOmniModel = GetOpt("jevomni-model", b.JevOmniModel),
            JevOmniHead = GetOpt("jevomni-head", b.JevOmniHead),
            JevOmniMmproj = GetOpt("jevomni-mmproj", b.JevOmniMmproj),
            FfmpegPath = GetOpt("ffmpeg", b.FfmpegPath),
            ServerExe = GetOpt("server-exe", b.ServerExe)
        };
    }
}
