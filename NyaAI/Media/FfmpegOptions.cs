namespace NyaAI.Media;

/// <summary>Настройки подготовки медиа через ffmpeg.</summary>
public sealed class FfmpegOptions
{
    /// <summary>Полный путь к ffmpeg.exe (или просто "ffmpeg", если он в PATH).</summary>
    public string? ExecutablePath { get; init; }

    /// <summary>Максимальная длина аудио, сек.</summary>
    public double AudioMaxSeconds { get; init; } = 30;

    /// <summary>Частота дискретизации аудио.</summary>
    public int AudioSampleRate { get; init; } = 16000;

    /// <summary>Число каналов аудио.</summary>
    public int AudioChannels { get; init; } = 1;

    /// <summary>Число кадров, извлекаемых из видео.</summary>
    public int VideoFrameCount { get; init; } = 16;
}
