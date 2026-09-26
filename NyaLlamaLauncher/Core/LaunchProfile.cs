using NyaLlamaLauncher.Core.Runners;

namespace NyaLlamaLauncher.Core;

/// <summary>
/// Одна сохранённая вариация запуска нейронки: движок + все параметры запуска.
/// </summary>
public sealed class LaunchProfile
{
    public string Id { get; set; } = NewId();
    public string Name { get; set; } = "Новый профиль";
    public string Runner { get; set; } = RunnerIds.LlamaCpp;

    // --- параметры llama.cpp ---
    public string ServerExe { get; set; } = @"F:\SOFT\llama.cpp\llama-server.exe";
    public string WorkingDir { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public string MmprojPath { get; set; } = "";
    public string TemplatePath { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8001;
    public int ContextSize { get; set; } = 65536;
    public string CacheTypeK { get; set; } = "q8_0";
    public string CacheTypeV { get; set; } = "q8_0";
    public bool FlashAttn { get; set; } = true;
    public string GpuLayers { get; set; } = "";
    public string Threads { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool WebUi { get; set; } = true;
    public bool Reasoning { get; set; } = true;
    public bool ContextShift { get; set; } = true;

    /// <summary>Режим эмбеддингов (--embedding --pooling none) для decision-моделей.</summary>
    public bool Embedding { get; set; } = false;

    /// <summary>Путь к ffmpeg.exe (для медиа decision-моделей; пусто = искать в PATH).</summary>
    public string FfmpegPath { get; set; } = "";

    /// <summary>Логический размер батча (--batch-size; пусто = по умолчанию).</summary>
    public string BatchSize { get; set; } = "";

    /// <summary>Физический размер батча (--ubatch-size; пусто = по умолчанию).</summary>
    public string UbatchSize { get; set; } = "";

    public string ExtraArgs { get; set; } = "";

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    public LaunchProfile Clone()
    {
        var c = (LaunchProfile)MemberwiseClone();
        c.Id = NewId();
        return c;
    }
}
