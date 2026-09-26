namespace NyaAI.Decision;

public enum DecisionMediaKind
{
    Image,
    Audio,
    Video
}

/// <summary>
/// Один вложенный медиафрагмент. Байты должны быть уже в формате, который ждёт
/// сервер: изображение — PNG/JPEG; аудио — WAV 16 кГц моно; видео — отдельный
/// кадр (PNG). Конвертацию (ffmpeg и т.п.) выполняет вызывающая сторона.
/// </summary>
public sealed record DecisionMedia(DecisionMediaKind Kind, byte[] Data)
{
    public static DecisionMedia Image(byte[] data) => new(DecisionMediaKind.Image, data);
    public static DecisionMedia Audio(byte[] wav16kMono) => new(DecisionMediaKind.Audio, wav16kMono);
    public static DecisionMedia VideoFrame(byte[] pngFrame) => new(DecisionMediaKind.Video, pngFrame);
}
