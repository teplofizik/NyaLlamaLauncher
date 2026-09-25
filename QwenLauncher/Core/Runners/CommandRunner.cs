namespace QwenLauncher.Core.Runners;

/// <summary>
/// Запуск произвольного сервера командой. Аргументы берутся из ExtraArgs,
/// программа — из ServerExe, каталог — из WorkingDir. Полезно для не-llama.cpp
/// серверов (например, Python-сервера Open-Jev).
/// </summary>
public sealed class CommandRunner : IModelRunner
{
    public string Id => RunnerIds.Command;
    public string DisplayName => "Команда (любой сервер)";

    public IReadOnlyList<string> BuildArguments(LaunchProfile c) =>
        ArgTokenizer.Split(c.ExtraArgs).ToList();

    public string ResolveExecutable(LaunchProfile c) => c.ServerExe;

    public string ResolveWorkingDirectory(LaunchProfile c) =>
        !string.IsNullOrWhiteSpace(c.WorkingDir)
            ? c.WorkingDir
            : Path.GetDirectoryName(c.ServerExe) ?? AppContext.BaseDirectory;

    public IReadOnlyList<string> Validate(LaunchProfile c)
    {
        var errors = new List<string>();

        var hasPath = c.ServerExe.Contains('\\') || c.ServerExe.Contains('/') || Path.IsPathRooted(c.ServerExe);
        if (string.IsNullOrWhiteSpace(c.ServerExe) || (hasPath && !File.Exists(c.ServerExe)))
            errors.Add("Не найдена программа: " + c.ServerExe);

        if (c.Port is < 1 or > 65535)
            errors.Add("Некорректный порт: " + c.Port);

        if (!string.IsNullOrWhiteSpace(c.WorkingDir) && !Directory.Exists(c.WorkingDir))
            errors.Add("Не найден рабочий каталог: " + c.WorkingDir);

        return errors;
    }

    public string BaseUrl(LaunchProfile c)
    {
        var host = c.Host;
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";
        return $"http://{host}:{c.Port}";
    }

    public string HealthUrl(LaunchProfile c) => BaseUrl(c) + "/health";
}
