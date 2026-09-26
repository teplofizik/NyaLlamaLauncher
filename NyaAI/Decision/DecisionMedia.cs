namespace NyaAI.Decision;

/// <summary>Тип медиафрагмента.</summary>
public enum DecisionMediaKind
{
    /// <summary>Изображение (PNG/JPEG/BMP/GIF; остальное нормализуется загрузчиком).</summary>
    Image,

    /// <summary>Аудио (WAV 16 кГц моно).</summary>
    Audio,

    /// <summary>Кадр видео (PNG).</summary>
    Video
}

/// <summary>
/// Один вложенный медиафрагмент. Байты должны быть уже в формате, который ждёт
/// сервер: изображение — PNG/JPEG/BMP/GIF; аудио — WAV 16 кГц моно; видео —
/// отдельный кадр (PNG). Подготовку выполняет <c>NyaAI.Media.DecisionMediaLoader</c>.
/// </summary>
/// <param name="Kind">Тип фрагмента.</param>
/// <param name="Data">Содержимое в нужном формате.</param>
public sealed record DecisionMedia(DecisionMediaKind Kind, byte[] Data)
{
    /// <summary>Создать фрагмент-изображение.</summary>
    public static DecisionMedia Image(byte[] data) => new(DecisionMediaKind.Image, data);

    /// <summary>Создать аудиофрагмент (WAV 16 кГц моно).</summary>
    public static DecisionMedia Audio(byte[] wav16kMono) => new(DecisionMediaKind.Audio, wav16kMono);

    /// <summary>Создать кадр видео (PNG).</summary>
    public static DecisionMedia VideoFrame(byte[] pngFrame) => new(DecisionMediaKind.Video, pngFrame);
}
