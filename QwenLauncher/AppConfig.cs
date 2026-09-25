using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QwenLauncher;

public sealed class AppConfig
{
    public string ServerExe { get; set; } = @"F:\SOFT\llama.cpp\llama-server.exe";
    public string ModelPath { get; set; } = @"F:\AI\Qwen3.8-27B-GGUF\Qwen3.8-27B-Q4_K_M.gguf";
    public string MmprojPath { get; set; } = @"F:\AI\Qwen3.8-27B-GGUF\mmproj-Qwen3.8-27B-BF16.gguf";
    public string TemplatePath { get; set; } = @"F:\AI\templates\qwen38-chat-template-corrected.jinja";
    public string Alias { get; set; } = "nya/Qwen3.8-27B-Q4_K_M";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8001;
    public int ContextSize { get; set; } = 65536;
    public string CacheTypeK { get; set; } = "q8_0";
    public string CacheTypeV { get; set; } = "q8_0";
    public bool FlashAttn { get; set; } = true;
    public string GpuLayers { get; set; } = "";
    public string Threads { get; set; } = "";
    public string ApiKey { get; set; } = "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ=";
    public bool WebUi { get; set; } = true;
    public bool Reasoning { get; set; } = true;
    public bool ContextShift { get; set; } = true;
    public string ExtraArgs { get; set; } = "";

    private const string Header =
        "# Qwen3.8 Launcher — конфигурация запуска llama-server\n" +
        "# Пути можно писать в Windows-виде (F:\\AI\\...) или через слэши (F:/AI/...).\n" +
        "# gpuLayers: пусто = автоподбор (--fit); число = -ngl N. threads: пусто = авто.\n" +
        "# extraArgs: произвольные доп. аргументы, добавляются в конец командной строки.\n" +
        "# Этот файл можно править вручную, а также он сохраняется из окна приложения.\n" +
        "\n";

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    [YamlIgnore]
    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.yaml");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var yaml = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var cfg = Deserializer.Deserialize<AppConfig>(yaml);
                if (cfg is not null) return cfg;
            }
        }
        catch
        {
            // on any parse error fall back to defaults and rewrite the file
        }

        var defaults = new AppConfig();
        defaults.Save();
        return defaults;
    }

    public void Save()
    {
        try
        {
            var body = Serializer.Serialize(this);
            File.WriteAllText(ConfigPath, Header + body, new UTF8Encoding(false));
        }
        catch
        {
            // ignore persistence errors
        }
    }
}
