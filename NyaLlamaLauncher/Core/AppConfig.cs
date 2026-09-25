using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using NyaLlamaLauncher.Core.Runners;

namespace NyaLlamaLauncher.Core;

/// <summary>
/// Корневой конфиг приложения: набор профилей запуска + выбранный профиль.
/// </summary>
public sealed class AppConfig
{
    public List<LaunchProfile> Profiles { get; set; } = new();
    public string SelectedProfileId { get; set; } = "";

    private const string Header =
        "# NyaLlama Launcher — конфигурация запуска нейронок\n" +
        "#\n" +
        "# Каждый профиль в profiles — отдельная нейронка/вариация запуска.\n" +
        "# runner: движок запуска (сейчас доступен \"llama.cpp\").\n" +
        "# Пути можно писать в Windows-виде (F:\\AI\\...) или через слэши (F:/AI/...).\n" +
        "# gpuLayers: пусто = автоподбор (--fit); число = -ngl N. threads: пусто = авто.\n" +
        "# extraArgs: произвольные доп. аргументы, добавляются в конец командной строки.\n" +
        "# Файл можно править вручную, а также он сохраняется из окна приложения.\n" +
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
        AppConfig? cfg = null;
        string? raw = null;

        try
        {
            if (File.Exists(ConfigPath))
            {
                raw = File.ReadAllText(ConfigPath, Encoding.UTF8);
                cfg = Deserializer.Deserialize<AppConfig>(raw);
            }
        }
        catch
        {
            cfg = null;
        }

        // новый формат без профилей -> пробуем мигрировать старый плоский конфиг
        if (cfg is null || cfg.Profiles.Count == 0)
        {
            cfg = TryMigrateLegacy(raw) ?? Default();
            cfg.Save();
        }

        if (cfg.Find(cfg.SelectedProfileId) is null)
            cfg.SelectedProfileId = cfg.Profiles[0].Id;

        return cfg;
    }

    private static AppConfig? TryMigrateLegacy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var legacy = Deserializer.Deserialize<LaunchProfile>(raw);
            if (legacy is null || string.IsNullOrWhiteSpace(legacy.ModelPath)) return null;

            legacy.Id = LaunchProfile.NewId();
            legacy.Runner = RunnerIds.LlamaCpp;
            if (string.IsNullOrWhiteSpace(legacy.Name) || legacy.Name == "Новый профиль")
                legacy.Name = "Qwen3.8-27B (local)";

            var cfg = new AppConfig();
            cfg.Profiles.Add(legacy);
            cfg.SelectedProfileId = legacy.Id;
            return cfg;
        }
        catch
        {
            return null;
        }
    }

    public static AppConfig Default()
    {
        var cfg = new AppConfig();
        const string exe = @"F:\SOFT\llama.cpp\llama-server.exe";
        const string key = "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ=";

        LaunchProfile Llama(string name, string alias, string model, int ctx, string cache, bool reasoning = false)
            => new()
            {
                Name = name,
                Runner = RunnerIds.LlamaCpp,
                ServerExe = exe,
                ModelPath = model,
                Alias = alias,
                Host = "127.0.0.1",
                Port = 8001,
                ContextSize = ctx,
                CacheTypeK = cache,
                CacheTypeV = cache,
                FlashAttn = true,
                WebUi = true,
                Reasoning = reasoning,
                ContextShift = true,
                ApiKey = key
            };

        var qwen = Llama("Qwen3.8-27B (local)", "nya/Qwen3.8-27B-Q4_K_M",
            @"F:\AI\Qwen3.8-27B-GGUF\Qwen3.8-27B-Q4_K_M.gguf", 65536, "q8_0", true);
        qwen.MmprojPath = @"F:\AI\Qwen3.8-27B-GGUF\mmproj-Qwen3.8-27B-BF16.gguf";
        qwen.TemplatePath = @"F:\AI\templates\qwen38-chat-template-corrected.jinja";

        // лёгкие coder-модели (влезают на GPU целиком)
        var ds = Llama("DeepSeek-Coder-6.7B", "local/deepseek-coder-6.7b",
            @"F:\AI\deepseek-coder-6.7B-kexer-Q4_K_M.gguf", 32768, "f16");
        var yi = Llama("Yi-Coder-9B-Chat", "local/yi-coder-9b",
            @"F:\AI\lmstudio-community\Yi-Coder-9B-Chat-GGUF\Yi-Coder-9B-Chat-Q4_K_M.gguf", 32768, "f16");
        var omni = Llama("OmniCoder-9B", "local/omnicoder-9b",
            @"F:\AI\Tesslate\OmniCoder-9B-GGUF\omnicoder-9b-q4_k_s.gguf", 32768, "f16");
        var q25 = Llama("Qwen2.5-Coder-14B", "local/qwen2.5-coder-14b",
            @"F:\AI\lmstudio-community\Qwen2.5-Coder-14B-Instruct-GGUF\Qwen2.5-Coder-14B-Instruct-Q4_K_M.gguf", 32768, "q8_0");
        var gemma = Llama("Gemma4-12B-Coder", "local/gemma4-12b-coding",
            @"F:\AI\yuxinlu1\gemma-4-12B-coder-fable5-composer2.5-v1-GGUF\gemma4-coding-Q4_K_M.gguf", 32768, "f16");

        // Jev — decision-модели (заготовки, файлы ещё не скачаны)
        var jevStyle = Llama("Jev-Style-2B Decision (Q8_0)", "jev-style-2b",
            @"F:\AI\Jev\Jev-Style-v2-Calibrated-Q8_0.gguf", 4096, "f16");
        var jevOmni = Llama("Jev-Omni Q4_K_M (мультимодал)", "jev-omni",
            @"F:\AI\Jev\Jev-Omni-Unified-Q4_K_M.gguf", 8192, "f16");
        jevOmni.MmprojPath = @"F:\AI\Jev\mmproj-jev-omni.gguf";
        jevOmni.ExtraArgs = "--embedding --pooling none --no-warmup";
        var openJev = Llama("OpenJev-27B Q4_K_M", "openjev-27b",
            @"F:\AI\Jev\OpenJev-Q4_K_M.gguf", 16384, "q8_0");

        var openJev9 = new LaunchProfile
        {
            Name = "Open-Jev-9B (Python, не llama.cpp)",
            Runner = RunnerIds.Command,
            ServerExe = "python",
            WorkingDir = @"F:\AI\Jev\Open-Jev",
            Alias = "open-jev-9b",
            Host = "127.0.0.1",
            Port = 8001,
            ExtraArgs = "-m jev.server --checkpoint ./checkpoints/open-jev-9b/package/checkpoint " +
                        "--device cuda:0 --max-length 4096 --batch-size 1 --no-prefix-cache " +
                        "--host 127.0.0.1 --port 8001"
        };

        cfg.Profiles.AddRange(new[]
        {
            qwen, ds, yi, omni, q25, gemma, jevStyle, jevOmni, openJev, openJev9
        });
        cfg.SelectedProfileId = qwen.Id;
        return cfg;
    }

    public LaunchProfile? Find(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Profiles.FirstOrDefault(p => p.Id == id);
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
            // игнорируем ошибки записи
        }
    }
}
