# NyaAI

![License: MIT](https://img.shields.io/badge/license-MIT-blue)

.NET-библиотека для локальных нейронок через `llama.cpp`:
обычная генерация текста, decision-модели Jev и подготовка медиа.

- Target: **net8.0**, без внешних зависимостей.
- Сервером не управляет — предполагается уже запущенный `llama-server`
  (например, профилем в `NyaLlamaLauncher`).
- Генерируется XML-документация (`NyaAI.xml`).

## Установка / подключение

```powershell
# из другого проекта в этом репозитории
dotnet add <проект> reference NyaAI\NyaAI.csproj
```

## Обзор

| Пространство | Назначение |
|---|---|
| `NyaAI.Llama` | клиент к llama.cpp: `/completion`, `/health`, `/v1/models`; генератор текста |
| `NyaAI.Generation` | абстракция генерации (`ITextGenerator`), чат, LLM-как-decision |
| `NyaAI.Decision` | абстракция решений (`IDecisionModel`), запрос/результат, медиа |
| `NyaAI.Jev` | Jev-Style и Jev-Omni decision-модели |
| `NyaAI.Media` | подготовка изображений/аудио/видео через ffmpeg |
| `NyaAI.Numpy` | чтение `.npy/.npz` (для decision-head) без зависимостей |

Поток данных: `ITextGenerator` / `IDecisionModel` → `LlamaServerClient` → llama.cpp.
Jev-Omni добавляет голову (`JevOmniHead` над `.npz`) поверх hidden-состояния.

## Быстрый старт

### 1. Генерация текста (обычная LLM)

```csharp
using NyaAI.Generation;
using NyaAI.Llama;

using var client = new LlamaServerClient(new LlamaServerOptions
{
    BaseUrl = "http://127.0.0.1:8001",
    ApiKey  = "<api-key>",
    Model   = "local/qwen2.5-coder-14b",          // alias сервера
    DefaultGeneration = new GenerationOptions { MaxTokens = 128, Temperature = 0.2 }
});
var llm = new LlamaTextGenerator(client);

// чат с ролями
var chat = await llm.ChatAsync(new[]
{
    ChatMessage.System("Отвечай кратко."),
    ChatMessage.User("Что такое мьютекс?")
});

// чистый completion
var raw = await llm.GenerateAsync("The capital of France is",
    new GenerationOptions { MaxTokens = 16, Temperature = 0 });

// потоковая выдача
await foreach (var tok in llm.ChatStreamAsync(new[] { ChatMessage.User("Считай до 5.") }))
    Console.Write(tok);
```

### 2. LLM как decision-модель

```csharp
using NyaAI.Decision;
using NyaAI.Generation;

var decider = new LlmDecisionModel(llm);   // промпт/разбор — LlmOptionPrompt
var r = await decider.DecideAsync(new DecisionRequest
{
    State = "The film was excellent.",
    Question = "Sentiment?",
    Options = new[] { "negative", "positive" }
});
Console.WriteLine($"{r.Best.Option} {r.Best.Probability:0.00}"); // positive 0.99

bool started = await decider.IsYesAsync("Meeting at 10 AM, now 9 AM.", "Has the meeting started?");
```

### 3. Jev-Style (decision через `/completion`)

```csharp
using NyaAI.Jev;

var jev = new JevStyleDecisionModel(client);
var choice = await jev.DecideAsync(new DecisionRequest
{
    State = "The film was excellent.",
    Question = "What is the sentiment of this review?",
    Options = new[] { "negative", "positive" }
});
// bool: Kind = DecisionKind.Bool, Options = { "yes", "no" }
// score: Kind = DecisionKind.Score, Options — от низшего к высшему
```

Калибровка в calibrated-GGUF уже вшита (температура 1.0). Если вариант не попал
в top-N, `JevDecisionOptions` увеличивает `n_probs` и пробует снова.

### 4. Jev-Omni (hidden + decision-head, медиа)

Сервер должен быть запущен с `--embedding --pooling none` (и `--mmproj` для медиа).

```csharp
using NyaAI.Decision;
using NyaAI.Jev;
using NyaAI.Media;

var omni = new JevOmniDecisionModel(client, @"F:\AI\Jev\decision-head-f32.npz");
var loader = new DecisionMediaLoader(new FfmpegOptions
{
    ExecutablePath = @"G:\Dev\ffmpeg-...\bin\ffmpeg.exe"
});

var r = await omni.DecideAsync(new DecisionRequest
{
    State = "Look at the image.",
    Question = "What color is the large shape?",
    Options = new[] { "blue", "red", "green", "yellow" },
    Media = new[] { await loader.LoadImageAsync("shape.png") }
});
```

Для видео `loader.LoadVideoAsync(path)` возвращает набор PNG-кадров, который
кладётся в `Media`.

## Медиа и ffmpeg

`DecisionMediaLoader` готовит вход под требования Jev-Omni:

| Вход | Метод | Результат |
|---|---|---|
| изображение png/jpg/jpeg/bmp/gif | `LoadImageAsync` | байты как есть |
| изображение webp/tiff/avif/heic/… | `LoadImageAsync` | **авто-перекодирование в PNG** (ffmpeg) |
| любое изображение | `LoadImageAsPngAsync` | принудительно PNG |
| аудио/видео | `LoadAudioAsync` | WAV 16 кГц моно |
| видео | `LoadVideoAsync` | равномерные PNG-кадры (по умолчанию 16) |

Проверено на mtmd: PNG/JPG/BMP/GIF читаются напрямую, WebP/TIFF — нет, поэтому
загрузчик нормализует их сам. ffmpeg ищется по `FfmpegOptions.ExecutablePath`, иначе
в `PATH`; `IsAvailable` показывает наличие.

> Видео-кадры дают много токенов — сервер стоит запускать с
> `--batch-size 2048 --ubatch-size 2048`, иначе будет «input too large».

## Обработка ошибок

Все ошибки — `NyaAI.Decision.NyaAIException` (HTTP-коды, парсинг, ffmpeg).
`LlamaServerClient.HealthAsync` возвращает `false` вместо исключения.

## Примеры

См. проект **`NyaAI.Examples`** — `generation`, `jev-style`, `jev-omni` на простых
ассетах из `Assets/`.
