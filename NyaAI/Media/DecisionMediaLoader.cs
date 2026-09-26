using System.Diagnostics;
using NyaAI.Decision;

namespace NyaAI.Media;

/// <summary>
/// Готовит медиа для decision-моделей: изображение — как есть; аудио — через
/// ffmpeg в WAV 16 кГц моно; видео — через ffmpeg в набор PNG-кадров.
/// Соответствует требованиям Jev-Omni (Gemma 4 Unified mmproj).
/// </summary>
public sealed class DecisionMediaLoader
{
    /// <summary>Имя исполняемого файла ffmpeg по умолчанию.</summary>
    public const string DefaultExecutable = "ffmpeg";

    private readonly FfmpegOptions _options;

    /// <summary>Создать загрузчик с настройками (null = по умолчанию).</summary>
    public DecisionMediaLoader(FfmpegOptions? options = null) =>
        _options = options ?? new FfmpegOptions();

    /// <summary>Найден ли ffmpeg (по заданному пути или в PATH).</summary>
    public bool IsAvailable => ResolveExecutable() is not null;

    /// <summary>Форматы, которые llama.cpp/mtmd читает напрямую (stb_image).</summary>
    private static readonly HashSet<string> PassThroughImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

    /// <summary>
    /// Загрузить изображение. Поддерживаемые форматы (png/jpg/jpeg/bmp/gif) идут
    /// как есть; остальные (webp, tiff, avif, heic и т.п.) перекодируются ffmpeg в PNG.
    /// Принудительно — через <see cref="LoadImageAsPngAsync"/>.
    /// </summary>
    /// <param name="path">Путь к изображению.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<DecisionMedia> LoadImageAsync(string path, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(path);
        if (PassThroughImageExtensions.Contains(ext))
            return DecisionMedia.Image(await File.ReadAllBytesAsync(path, cancellationToken));

        var png = await TranscodeImageToPngAsync(path, cancellationToken);
        return DecisionMedia.Image(png);
    }

    /// <summary>Всегда перекодировать изображение в PNG через ffmpeg.</summary>
    /// <param name="path">Путь к изображению.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<DecisionMedia> LoadImageAsPngAsync(string path, CancellationToken cancellationToken = default)
    {
        var png = await TranscodeImageToPngAsync(path, cancellationToken);
        return DecisionMedia.Image(png);
    }

    private async Task<byte[]> TranscodeImageToPngAsync(string path, CancellationToken cancellationToken)
    {
        var exe = RequireExecutable();
        var args = new[]
        {
            "-v", "error", "-y",
            "-i", path,
            "-frames:v", "1",
            "-f", "image2", "-c:v", "png",
            "pipe:1"
        };
        var bytes = await RunAsync(exe, args, cancellationToken);
        if (bytes.Length == 0) throw new NyaAIException("ffmpeg вернул пустое изображение.");
        return bytes;
    }

    /// <summary>Загрузить аудио: ffmpeg → WAV 16 кГц моно.</summary>
    /// <param name="path">Путь к аудио/видеофайлу.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<DecisionMedia> LoadAudioAsync(string path, CancellationToken cancellationToken = default)
    {
        var exe = RequireExecutable();
        var args = new[]
        {
            "-v", "error", "-y",
            "-i", path,
            "-t", _options.AudioMaxSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-ac", _options.AudioChannels.ToString(),
            "-ar", _options.AudioSampleRate.ToString(),
            "-f", "wav",
            "pipe:1"
        };

        var bytes = await RunAsync(exe, args, cancellationToken);
        if (bytes.Length == 0) throw new NyaAIException("ffmpeg вернул пустой аудиопоток.");
        return DecisionMedia.Audio(bytes);
    }

    /// <summary>Извлечь равномерно распределённые PNG-кадры видео (по умолчанию <see cref="FfmpegOptions.VideoFrameCount"/>).</summary>
    /// <param name="path">Путь к видеофайлу.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<IReadOnlyList<DecisionMedia>> LoadVideoAsync(string path, CancellationToken cancellationToken = default)
    {
        var fps = GetFps(path, cancellationToken);
        var count = Math.Max(1, _options.VideoFrameCount);

        // равномерные моменты времени, как в эталонном клиенте: (total-1)*(k+0.5)/count
        var duration = GetDuration(path, cancellationToken);
        if (duration <= 0) throw new NyaAIException($"Не удалось определить длительность видео: {path}");

        var frames = new List<DecisionMedia>(count);
        for (int k = 0; k < count; k++)
        {
            var time = duration * (k + 0.5) / count;
            var png = await ExtractFrameAsync(path, time, cancellationToken);
            if (png.Length > 0) frames.Add(DecisionMedia.VideoFrame(png));
        }

        if (frames.Count == 0) throw new NyaAIException($"Не удалось извлечь ни одного кадра из видео: {path}");
        return frames;
    }

    private async Task<byte[]> ExtractFrameAsync(string path, double time, CancellationToken cancellationToken)
    {
        var exe = RequireExecutable();
        var seconds = time.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        var args = new[]
        {
            "-v", "error", "-y",
            "-ss", seconds,
            "-i", path,
            "-frames:v", "1",
            "-f", "image2", "-c:v", "png",
            "pipe:1"
        };
        return await RunAsync(exe, args, cancellationToken);
    }

    private double GetDuration(string path, CancellationToken cancellationToken)
    {
        // через ffmpeg -i в stderr ищем "Duration: HH:MM:SS.xx"
        var exe = RequireExecutable();
        var psi = CreateStartInfo(exe, new[] { "-hide_banner", "-i", path });
        using var proc = Process.Start(psi)!;
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var m = System.Text.RegularExpressions.Regex.Match(stderr, @"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)");
        if (!m.Success) return 0;
        return int.Parse(m.Groups[1].Value) * 3600
             + int.Parse(m.Groups[2].Value) * 60
             + double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private int GetFps(string path, CancellationToken cancellationToken)
    {
        var exe = RequireExecutable();
        var psi = CreateStartInfo(exe, new[] { "-hide_banner", "-i", path });
        using var proc = Process.Start(psi)!;
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var m = System.Text.RegularExpressions.Regex.Match(stderr, @"(\d+(?:\.\d+)?)\s*fps");
        if (!m.Success) return 25;
        if (double.TryParse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var fps) && fps > 0)
            return (int)Math.Round(fps);
        return 25;
    }

    private static ProcessStartInfo CreateStartInfo(string exe, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }

    private static async Task<byte[]> RunAsync(string exe, IEnumerable<string> args, CancellationToken cancellationToken)
    {
        var psi = CreateStartInfo(exe, args);
        psi.RedirectStandardOutput = true;

        using var proc = Process.Start(psi)!;
        using var ms = new MemoryStream();

        var copy = proc.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);
        var errTask = proc.StandardError.ReadToEndAsync(cancellationToken);
        await copy;
        var err = await errTask;
        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            var tail = err.Length > 800 ? err[^800..] : err;
            throw new NyaAIException($"ffmpeg завершился с кодом {proc.ExitCode}: {tail}");
        }

        return ms.ToArray();
    }

    private string? ResolveExecutable()
    {
        var configured = _options.ExecutablePath;
        if (!string.IsNullOrWhiteSpace(configured))
            return File.Exists(configured) ? configured : null;

        // поиск в PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var dir in paths)
        {
            try
            {
                var candidate = Path.Combine(dir, "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* ignore */ }
        }
        return null;
    }

    private string RequireExecutable() =>
        ResolveExecutable() ?? throw new NyaAIException(
            "ffmpeg не найден. Укажите полный путь в FfmpegOptions.ExecutablePath или добавьте ffmpeg в PATH.");
}
