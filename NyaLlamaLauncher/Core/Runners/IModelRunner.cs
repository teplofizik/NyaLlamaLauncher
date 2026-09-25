namespace NyaLlamaLauncher.Core.Runners;

/// <summary>
/// Подключаемый движок запуска нейронки. Новый тип нейронки = новая
/// реализация этого интерфейса, зарегистрированная в <see cref="RunnerRegistry"/>.
/// </summary>
public interface IModelRunner
{
    /// <summary>Стабильный идентификатор движка (хранится в профиле).</summary>
    string Id { get; }

    /// <summary>Отображаемое имя в UI.</summary>
    string DisplayName { get; }

    /// <summary>Собрать аргументы командной строки для процесса.</summary>
    IReadOnlyList<string> BuildArguments(LaunchProfile profile);

    /// <summary>Путь к исполняемому файлу сервера.</summary>
    string ResolveExecutable(LaunchProfile profile);

    /// <summary>Рабочая директория процесса (обычно каталог exe, нужен для DLL).</summary>
    string ResolveWorkingDirectory(LaunchProfile profile);

    /// <summary>Проверить профиль перед запуском. Пустой список = всё ок.</summary>
    IReadOnlyList<string> Validate(LaunchProfile profile);

    /// <summary>Базовый HTTP-адрес сервера, например http://127.0.0.1:8001.</summary>
    string BaseUrl(LaunchProfile profile);

    /// <summary>URL проверки готовности.</summary>
    string HealthUrl(LaunchProfile profile);
}

public static class RunnerIds
{
    public const string LlamaCpp = "llama.cpp";

    /// <summary>Запуск произвольной команды-сервера (python, exe и т.п.).</summary>
    public const string Command = "command";
}
