using NyaAI.Decision;
using NyaAI.Jev;
using NyaAI.Llama;
using NyaAI.Media;

namespace NyaAI.Examples;

/// <summary>
/// Пример Jev-Omni: решения по тексту, изображению, аудио и видео из Assets.
/// Требует запуска сервера с --embedding --pooling none и (для медиа) mmproj.
/// </summary>
public static class JevOmniExample
{
    public static async Task RunAsync(ExampleConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.JevOmniModel) || string.IsNullOrWhiteSpace(cfg.JevOmniHead))
        {
            Console.WriteLine("  Jev-Omni не задан (--jevomni-model=... --jevomni-head=...), пропуск.");
            return;
        }

        var assets = ExampleConfig.AssetsDir;
        var bootstrapArgs = new List<string>
        {
            "--model", cfg.JevOmniModel,
            "--alias", "jev-omni", "--jinja",
            "--host", "127.0.0.1", "--port", new Uri(cfg.BaseUrl).Port.ToString(),
            "--ctx-size", "8192", "--parallel", "1",
            "--batch-size", "2048", "--ubatch-size", "2048",
            "--flash-attn", "on", "-ngl", "99", "--no-warmup",
            "--api-key", cfg.ApiKey,
            "--embedding", "--pooling", "none"
        };

        var mmproj = ResolveMmproj(cfg);
        if (!string.IsNullOrWhiteSpace(mmproj))
        {
            bootstrapArgs.Add("--mmproj");
            bootstrapArgs.Add(mmproj);
            bootstrapArgs.Add("--no-mmproj-offload");
            Console.WriteLine($"  mmproj: {mmproj}");
        }
        else
        {
            Console.WriteLine("  mmproj не найден — медиа будет недоступно (только текст).");
        }

        using var server = await ServerBootstrap.EnsureAsync(cfg, bootstrapArgs);

        using var client = new LlamaServerClient(new LlamaServerOptions { BaseUrl = cfg.BaseUrl, ApiKey = cfg.ApiKey });
        var model = new JevOmniDecisionModel(client, cfg.JevOmniHead!);
        var loader = new DecisionMediaLoader(new FfmpegOptions { ExecutablePath = cfg.FfmpegPath });

        // текст
        var text = await model.DecideAsync(new DecisionRequest
        {
            State = "The meeting starts at 10 AM. It is now 9 AM.",
            Question = "Has the meeting started?",
            Options = new[] { "yes", "no" },
            Kind = DecisionKind.Bool
        });
        Console.WriteLine($"  [text ] started={text.Best.Option} yes={text.YesProbability:0.000}");

        // изображение (png проходит как есть)
        var png = await loader.LoadImageAsync(Path.Combine(assets, "shape_red.png"));
        var img = await model.DecideAsync(new DecisionRequest
        {
            State = "Look at the image.",
            Question = "What color is the large shape?",
            Options = new[] { "blue", "red", "green", "yellow" },
            Media = new[] { png }
        });
        Console.WriteLine($"  [image] {img.Best.Option} p={img.Best.Probability:0.000}");

        // изображение в неподдерживаемом формате (webp) -> автоперекодирование ffmpeg
        var webpPath = Path.Combine(assets, "shape_red.webp");
        if (File.Exists(webpPath) && loader.IsAvailable)
        {
            var webp = await loader.LoadImageAsync(webpPath);
            var img2 = await model.DecideAsync(new DecisionRequest
            {
                State = "Look at the image.",
                Question = "What color is the large shape?",
                Options = new[] { "blue", "red", "green", "yellow" },
                Media = new[] { webp }
            });
            Console.WriteLine($"  [webp ] {img2.Best.Option} p={img2.Best.Probability:0.000} (перекодировано в PNG)");
        }
        else
        {
            Console.WriteLine("  [webp ] пропуск (нет Assets/shape_red.webp или ffmpeg).");
        }

        // аудио
        var audioPath = Path.Combine(assets, "tone_440.mp3");
        if (loader.IsAvailable && File.Exists(audioPath))
        {
            var audio = await loader.LoadAudioAsync(audioPath);
            var a = await model.DecideAsync(new DecisionRequest
            {
                State = "Listen to the audio.",
                Question = "Is this a steady single tone?",
                Options = new[] { "yes", "no" },
                Kind = DecisionKind.Bool,
                Media = new[] { audio }
            });
            Console.WriteLine($"  [audio] steady={a.Best.Option} yes={a.YesProbability:0.000}");
        }
        else
        {
            Console.WriteLine("  [audio] пропуск (нет Assets/tone_440.mp3 или ffmpeg).");
        }

        // видео
        var videoPath = Path.Combine(assets, "clip_redbox.mp4");
        if (loader.IsAvailable && File.Exists(videoPath))
        {
            var frames = await loader.LoadVideoAsync(videoPath);
            var v = await model.DecideAsync(new DecisionRequest
            {
                State = "Watch the video.",
                Question = "What color is the moving object?",
                Options = new[] { "red", "blue", "green", "black" },
                Media = frames
            });
            Console.WriteLine($"  [video] {v.Best.Option} p={v.Best.Probability:0.000} ({frames.Count} кадров)");
        }
        else
        {
            Console.WriteLine("  [video] пропуск (нет Assets/clip_redbox.mp4 или ffmpeg).");
        }
    }

    /// <summary>Ищет mmproj рядом с моделью/головой или по имени по папкам модели.</summary>
    private static string? ResolveMmproj(ExampleConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.JevOmniMmproj) && File.Exists(cfg.JevOmniMmproj))
            return cfg.JevOmniMmproj;

        var candidates = new List<string>();
        if (cfg.JevOmniModel is { Length: > 0 } modelDir)
        {
            var dir = Path.GetDirectoryName(modelDir)!;
            candidates.Add(Path.Combine(dir, "mmproj-jev-omni.gguf"));
            candidates.Add(Path.Combine(dir, "Jev", "mmproj-jev-omni.gguf"));
        }
        if (cfg.JevOmniHead is { Length: > 0 } head)
        {
            candidates.Add(Path.Combine(Path.GetDirectoryName(head)!, "mmproj-jev-omni.gguf"));
        }

        return candidates.FirstOrDefault(File.Exists);
    }
}
