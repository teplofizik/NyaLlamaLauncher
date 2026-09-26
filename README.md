# NyaLlama Launcher

![License: MIT](https://img.shields.io/badge/license-MIT-blue)

GUI-лаунчер и набор библиотек для локального запуска нейронок (`llama.cpp`).

Состав репозитория:
- **`NyaLlamaLauncher`** — WinForms-лаунчер (профили запуска, движки).
- **`NyaAI`** — .NET-библиотека: генерация текста, decision-модели Jev, работа с медиа. См. [`NyaAI/README.md`](NyaAI/README.md).
- **`NyaAI.Examples`** — примеры и тестовые ассеты. См. [`NyaAI.Examples/README.md`](NyaAI.Examples/README.md).
- **`Launcher`** — публикация лаунчера (`dotnet publish`).
- Лицензия — [`LICENSE`](LICENSE) (MIT).

GUI-лаунчер для локального запуска GGUF-моделей через `llama.cpp` (`llama-server`)
на `127.0.0.1`, с профилями под разные нейронки и подключаемыми движками (runners).

- Приложение: `Launcher\NyaLlamaLauncher.exe` (ярлык «NyaLlama Launcher» на рабочем столе)
- Конфиг: `Launcher\config.yaml` (список профилей + выбранный профиль)
- Логика: `NyaLlamaLauncher\Core` (профили, раннеры, процесс) — GUI: `NyaLlamaLauncher\UI`
- Сервер: `F:\SOFT\llama.cpp\llama-server.exe` (llama.cpp **b11195 / v0.5.0-dev**, CUDA 13.4;
  бэкап прежней сборки b9219 — `F:\SOFT\llama.cpp\_backup_b9219`)
- Порт у всех профилей — **8001**, хост **127.0.0.1**, API-ключ общий, поэтому
  opencode настраивается один раз, а меняется только модель.

## Как пользоваться

1. Запустить `NyaLlamaLauncher.exe` (или ярлык).
2. Выбрать нейронку в списке «Нейронка» сверху; поля заполнятся последней
   сохранённой вариацией. `＋ Добавить` / `⧉ Дублировать` / `🗑 Удалить` — управление профилями.
3. «▶ Запустить» → дождаться статуса «Готов → http://127.0.0.1:8001».
4. В opencode выбрать нужную модель (`/models`). Кнопка «⧉ Config opencode»
   копирует готовый JSON провайдера для текущего профиля.

Запускается только один сервер за раз; профили переключаются на лету (сервер нужно
остановить, затем выбрать другой профиль и запустить снова).

## Профили по умолчанию

| Профиль | Модель | Размер | Влезает в 16 ГБ | Репозиторий |
|---|---|---:|---|---|
| Qwen3.8-27B (local) | Qwen3.8-27B-Q4_K_M | 15.7 ГБ | частично (часть на CPU) | — |
| DeepSeek-Coder-6.7B | deepseek-coder-6.7B-kexer-Q4_K_M | 3.8 ГБ | да | [lmstudio-community/deepseek-coder-6.7B-kexer-GGUF](https://huggingface.co/lmstudio-community/deepseek-coder-6.7B-kexer-GGUF) |
| Yi-Coder-9B-Chat | Yi-Coder-9B-Chat-Q4_K_M | 5.0 ГБ | да | [lmstudio-community/Yi-Coder-9B-Chat-GGUF](https://huggingface.co/lmstudio-community/Yi-Coder-9B-Chat-GGUF) |
| OmniCoder-9B | omnicoder-9b-q4_k_s | 5.0 ГБ | да | [Tesslate/OmniCoder-9B-GGUF](https://huggingface.co/Tesslate/OmniCoder-9B-GGUF) |
| Qwen2.5-Coder-14B | Qwen2.5-Coder-14B-Instruct-Q4_K_M | 8.4 ГБ | да | [lmstudio-community/Qwen2.5-Coder-14B-Instruct-GGUF](https://huggingface.co/lmstudio-community/Qwen2.5-Coder-14B-Instruct-GGUF) |
| Gemma4-12B-Coder | gemma4-coding-Q4_K_M | 6.9 ГБ | да | [yuxinlu1/gemma-4-12B-coder-fable5-composer2.5-v1-GGUF](https://huggingface.co/yuxinlu1/gemma-4-12B-coder-fable5-composer2.5-v1-GGUF) |
| Jev-Style-2B Decision (BF16) | Jev-Style-v2-Calibrated-BF16 | 3.8 ГБ | да | [chaoliangUNSW/Jev-Style-Qwen3.5-2B-Decision-v2-GGUF](https://huggingface.co/chaoliangUNSW/Jev-Style-Qwen3.5-2B-Decision-v2-GGUF) |
| Jev-Omni Q4_K_M (мультимодал) | Jev-Omni-Unified-Q4_K_M + mmproj | 6.9 ГБ | да | [Reza2kn/Jev-Omni-Q4_K_M-GGUF](https://huggingface.co/Reza2kn/Jev-Omni-Q4_K_M-GGUF) |
| OpenJev-27B Q4_K_M | OpenJev-Q4_K_M | 16.5 ГБ | нет (нужно 24 ГБ) | [openjev/openjev-GGUF](https://huggingface.co/openjev/openjev-GGUF) |
| Open-Jev-9B (Python) | LoRA+head для Qwen3.5-9B | — | — | [ZefanCai/Open-Jev-9B](https://huggingface.co/ZefanCai/Open-Jev-9B) |

> **Jev — это не чат-ассистенты, а decision/classification-модели.** Они принимают
> «state + question + варианты» и возвращают выбранный вариант с вероятностями.
> Свободный текст, код и tool-calling они не генерируют, поэтому как основная
> модель для opencode не подходят. Для «несложных задач» используйте лёгкие
> coder-модели выше.

### Замеры на этой машине (RTX 5080 16 ГБ, 2×Xeon E5-2650 v3)

| Модель | ctx | KV | VRAM | prefill | генерация |
|---|---:|---|---:|---:|---:|
| Qwen3.8-27B Q4_K_M | 65536 | q8_0 | ~15.1 ГиБ (часть на CPU) | ~17 т/с | ~3.8 т/с |
| Qwen2.5-Coder-14B Q4_K_M | 32768 | q8_0 | 14.2 ГиБ | 364 т/с | 42 т/с |
| Gemma4-12B-Coder Q4_K_M | 32768 | f16 | 11.0 ГиБ | 227 т/с | 52 т/с |

Для 14B KV-кэш `f16` на 32k не влезает в 16 ГБ (модель уходит частично на CPU и
падает до ~1.6 т/с) — поэтому в профиле стоит `q8_0`. У моделей 9B и меньше
`f16` помещается целиком.

## Скачивание

Скачанные модели кладите по путям, указанным в профилях (`config.yaml`), либо
поправьте пути в окне приложения.

```bash
# лёгкие coder-модели (уже есть в F:\AI, ссылки для повторной загрузки)
hf download lmstudio-community/deepseek-coder-6.7B-kexer-GGUF --local-dir F:/AI/lmstudio-community/deepseek-coder-6.7B-kexer-GGUF
hf download lmstudio-community/Yi-Coder-9B-Chat-GGUF --local-dir "F:/AI/lmstudio-community/Yi-Coder-9B-Chat-GGUF"
hf download Tesslate/OmniCoder-9B-GGUF --local-dir "F:/AI/Tesslate/OmniCoder-9B-GGUF"
hf download lmstudio-community/Qwen2.5-Coder-14B-Instruct-GGUF --local-dir "F:/AI/lmstudio-community/Qwen2.5-Coder-14B-Instruct-GGUF"
hf download yuxinlu1/gemma-4-12B-coder-fable5-composer2.5-v1-GGUF --local-dir "F:/AI/yuxinlu1/gemma-4-12B-coder-fable5-composer2.5-v1-GGUF"

# Jev (decision-модели) — ожидаемый каталог F:\AI\Jev
hf download chaoliangUNSW/Jev-Style-Qwen3.5-2B-Decision-v2-GGUF --local-dir F:/AI/Jev
hf download Reza2kn/Jev-Omni-Q4_K_M-GGUF --local-dir F:/AI/Jev
hf download openjev/openjev-GGUF --local-dir F:/AI/Jev
```

Для прямого скачивания одного файла можно использовать:
`https://huggingface.co/<repo>/resolve/main/<file>?download=true`.

Без установленного `hf` (huggingface_hub) используйте `pip install -U huggingface_hub`.

## Подключение к opencode

Сервер поднимается на `http://127.0.0.1:8001` с API-ключом
`+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ=` и OpenAI-совместимым API
(`/v1/chat/completions`, `/v1/models`). Провайдер в `~/.config/opencode/opencode.json`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "provider": {
    "llama.cpp": {
      "npm": "@ai-sdk/openai-compatible",
      "options": {
        "baseURL": "http://127.0.0.1:8001/v1",
        "apiKey": "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ="
      },
      "models": {
        "local/qwen2.5-coder-14b": { "name": "Qwen2.5-Coder-14B", "limit": { "context": 32768, "output": 8192 } }
      }
    }
  }
}
```

Идентификатор модели в opencode = `llama.cpp/<alias>`, где `<alias>` — поле
«Алиас модели» из профиля (например `llama.cpp/local/qwen2.5-coder-14b`).
После правки конфига opencode нужно **перезапустить**.

## Библиотека NyaAI (код для Jev и не только)

`NyaAI\` — .NET-библиотека (`NyaAI.dll`, net8.0) с абстракцией и клиентами для
локальных нейронок. Сейчас реализованы decision-модели Jev-Style (через native
`/completion`), в перспективе — Jev-Omni, openjev и др.

```
NyaAI/
  Decision/    IDecisionModel, DecisionRequest/Result, DecisionKind, IsYesAsync, ILlmDecisionPrompt
  Llama/       LlamaServerClient (/completion,/health,/v1/models), LlamaTextGenerator
  Generation/  ITextGenerator, ChatMessage, GenerationOptions/Result, LlmDecisionModel, LlmOptionPrompt
  Jev/         JevStyleDecisionModel, JevStylePrompt, JevDecisionOptions
```

Два уровня генерации текста: низкий — `GenerateAsync(промпт)` (native `/completion`)
и удобный — `ChatAsync(messages)` (`/v1/chat/completions`); оба с потоковыми версиями.

Пример использования (choice / bool / score):

```csharp
using NyaAI.Decision;
using NyaAI.Jev;
using NyaAI.Llama;

using var client = new LlamaServerClient(new LlamaServerOptions
{
    BaseUrl = "http://127.0.0.1:8001",
    ApiKey  = "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ="
});
var model = new JevStyleDecisionModel(client);

var r = await model.DecideAsync(new DecisionRequest
{
    State = "The film was excellent.",
    Question = "Sentiment?",
    Options = new[] { "negative", "positive" },
    Kind = DecisionKind.Choice
});
Console.WriteLine($"{r.Best.Option} p={r.Best.Probability:0.0000}");

bool started = await model.IsYesAsync("Meeting at 10 AM, now 9 AM.", "Has the meeting started?");
```

Проверено на `Jev-Style-v2-Calibrated-BF16.gguf`: choice `positive 0.997`,
bool возвращает корректный ответ, score — ожидаемый балл.

### Jev-Omni (decision-head)

Jev-Omni — не генератор и не Jev-Style: сервер поднимается с `--embedding --pooling none`,
берётся последний hidden-вектор (3840), и к нему применяется отдельная FP32-голова
`decision-head-f32.npz`. Класс `NyaAI.Jev.JevOmniDecisionModel` делает это сам
(читает `.npz` встроенным парсером, HTTP — через `LlamaServerClient`):

```csharp
var omni = new JevOmniDecisionModel(client, @"F:\AI\Jev\decision-head-f32.npz");
var r = await omni.DecideAsync(new DecisionRequest
{
    State = "The meeting starts at 10 AM. It is now 9 AM.",
    Question = "Has the meeting started?",
    Options = new[] { "yes", "no" },
    Kind = DecisionKind.Bool
});
Console.WriteLine($"{r.Best.Option} yes={r.YesProbability:0.000}");
```

Запуск сервера — профиль «Jev-Omni Q4_K_M (мультимодал)» в лаунчере (галочка «Эмбеддинги»).
Проверено на `Jev-Omni-Unified-Q4_K_M.gguf`: bool/choice/score и решение по изображению работают.

Медиа передаётся через `DecisionRequest.Media` (`DecisionMedia.Image/Audio/VideoFrame`,
байты уже в нужном формате):

```csharp
var img = await omni.DecideAsync(new DecisionRequest
{
    State = "Look at the image.",
    Question = "What color is the large shape?",
    Options = new[] { "blue", "red", "green", "yellow" },
    Media = new[] { DecisionMedia.Image(File.ReadAllBytes(@"C:\path\pic.png")) }
});
```

> Для медиа нужен mmproj `mmproj-jev-omni.gguf` (уже в профиле). Сборка llama.cpp
> обновлена до **b11195** — она понимает тип проектора `gemma4uv`. Проверено:
> `/props` отдаёт `vision/audio/video = true` и `media_marker`.

### Подготовка медиа (ffmpeg)

`NyaAI.Media.DecisionMediaLoader` готовит медиа под требования Jev-Omni:

- изображение — байты как есть (`LoadImageAsync`);
- аудио — `LoadAudioAsync` → WAV 16 кГц моно (ffmpeg);
- видео — `LoadVideoAsync` → равномерные PNG-кадры (ffmpeg), по умолчанию 16.

```csharp
var loader = new DecisionMediaLoader(new FfmpegOptions
{
    ExecutablePath = @"G:\Dev\ffmpeg-2023-12-14-git-5256b2fbe6-essentials_build\bin\ffmpeg.exe"
});
var audio  = await loader.LoadAudioAsync(@"C:\path\speech.mp3");
var frames = await loader.LoadVideoAsync(@"C:\path\clip.mp4");
```

Путь к ffmpeg указывается в профиле лаунчера — поле `ffmpegPath` (в UI «ffmpeg.exe (медиа)»),
для профиля Jev-Omni прописан `G:\Dev\ffmpeg-2023-12-14-git-5256b2fbe6-essentials_build\bin\ffmpeg.exe`.
Подойдёт любая x64-сборка ffmpeg (проверено на 2023-12-14 gyan.dev, full/essentials).
Если путь не задан — ищется `ffmpeg.exe` в `PATH`.

> Видео-кадры дают много токенов: для профиля Jev-Omni в конфиге заданы
> `batchSize`/`ubatchSize = 2048`, иначе сервер вернёт «input too large» на длинном
> медиа-промпте. Проверено: аудио и видео end-to-end работают.

### Генерация текста (обычные LLM)

```csharp
using NyaAI.Generation;
using NyaAI.Llama;

using var client = new LlamaServerClient(new LlamaServerOptions
{
    BaseUrl = "http://127.0.0.1:8001",
    ApiKey  = "+ynqf5MKHHKQ#aFm+T7JK@0xg4BN7^4%QyLNVGB%fJQ=",
    Model   = "local/qwen2.5-coder-14b"
});
var llm = new LlamaTextGenerator(client);

// чат с ролями
var chat = await llm.ChatAsync(new[]
{
    ChatMessage.System("Отвечай кратко."),
    ChatMessage.User("Что такое мьютекс?")
});

// чистый completion
var raw = await llm.GenerateAsync("The capital of France is", new GenerationOptions { MaxTokens = 16 });

// потоковая выдача
await foreach (var tok in llm.ChatStreamAsync(new[] { ChatMessage.User("Считай до 5.") }))
    Console.Write(tok);
```

Любую LLM можно использовать как decision-модель (для решений в обработке данных):

```csharp
var decider = new LlmDecisionModel(llm);   // промпт и разбор — LlmOptionPrompt
var r = await decider.DecideAsync(new DecisionRequest
{
    State = "The film was excellent.",
    Question = "Sentiment?",
    Options = new[] { "negative", "positive" }
});
Console.WriteLine($"{r.Best.Option} {r.Best.Probability:0.00}"); // positive 0.99

bool started = await decider.IsYesAsync("Meeting at 10 AM, now 9 AM.", "Has the meeting started?");
```

Проверено на Qwen2.5-Coder-14B: чат ~56 т/с, стриминг и «LLM как decision» работают.

> Для чата у модели должен быть корректный чат-шаблон в GGUF (`--jinja`). Некоторые
> completion-модели (например `deepseek-coder-6.7B-kexer`) без шаблона «продолжают
> диалог» — для них используйте `GenerateAsync` либо задайте `--chat-template-file`.

### Проект примеров NyaAI.Examples

`NyaAI.Examples\` — консольные примеры для проверки классов. Запуск:

```powershell
# все примеры
dotnet run --project NyaAI.Examples

# конкретный: generation | jev-style | jev-omni
dotnet run --project NyaAI.Examples -- jev-omni
```

Пути/модели задаются аргументами `--key=value` или переменными окружения
(`NYAAI_*`). Примеры могут сами поднять `llama-server`, если задан
`NYAAI_SERVER_EXE`:

```powershell
$env:NYAAI_SERVER_EXE   = "F:\SOFT\llama.cpp\llama-server.exe"
$env:NYAAI_JEVOMNI_MODEL= "F:\AI\Jev-Omni-Unified-Q4_K_M.gguf"
$env:NYAAI_JEVOMNI_HEAD = "F:\AI\Jev\decision-head-f32.npz"
$env:NYAAI_FFMPEG       = "G:\Dev\ffmpeg-2023-12-14-git-5256b2fbe6-essentials_build\bin\ffmpeg.exe"
$env:NYAAI_LLM_MODEL    = "F:\AI\lmstudio-community\Qwen2.5-Coder-14B-Instruct-GGUF\Qwen2.5-Coder-14B-Instruct-Q4_K_M.gguf"
$env:NYAAI_LLM_ALIAS    = "local/qwen2.5-coder-14b"
dotnet run --project NyaAI.Examples
```

Медиа-ассеты лежат в `NyaAI.Examples\Assets` (простые фото/аудио/видео, по которым
проходят нейронки). Они генерируются скриптом `NyaAI.Examples\tools\make-assets.py`
(Pillow + ffmpeg), в git не хранятся.

| Пример | Что проверяет |
|---|---|
| `generation` | обычная LLM: completion, чат, стриминг, LLM-decision |
| `jev-style` | Jev-Style: choice/bool/score через `/completion` |
| `jev-omni` | Jev-Omni: текст + изображение (png/webp) + аудио + видео |

### Формат изображений

Проверено на mtmd: **PNG, JPG/JPEG, BMP, GIF** читаются напрямую; **WebP и TIFF —
нет** («Failed to load image»). Поэтому `DecisionMediaLoader.LoadImageAsync`
пропускает поддерживаемые форматы как есть, а остальные (webp/tiff/avif/heic…)
автоматически перекодирует в PNG через ffmpeg. Принудительно —
`LoadImageAsPngAsync`. Отдельный «оговорённый» формат не требуется: достаточно
png/jpg, остальное нормализуется само.

Подключить к своему проекту:

```powershell
dotnet add <проект> reference NyaAI\NyaAI.csproj
```

> Калиброванная температура для calibrated-GGUF — 1.0 (клиент использует
> `temperature` только для шага генерации, нормировка идёт по логитам).

## Расширение

- Новый профиль — кнопкой в UI или блоком `- id: ...` в `profiles:` (`config.yaml`).
- Новый движок (не llama.cpp) — реализовать `IModelRunner` и добавить экземпляр
  в `NyaLlamaLauncher\Core\Runners\RunnerRegistry.cs`. Для произвольных команд уже
  есть раннер `command` (профиль Open-Jev-9B): `serverExe` = программа,
  `extraArgs` = аргументы, `workingDir` = рабочий каталог.

## Сборка

```powershell
dotnet publish NyaLlamaLauncher\NyaLlamaLauncher.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Launcher
```

Требуется .NET 9 Desktop Runtime (на машине установлен).
