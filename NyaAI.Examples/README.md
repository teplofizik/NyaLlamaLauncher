# NyaAI.Examples

Консольные примеры для проверки классов библиотеки **NyaAI** на простых
медиа-файлах из `Assets/`.

## Что внутри

| Пример | Что проверяет | Нужно задать |
|---|---|---|
| `generation` | обычная LLM: completion, чат, стриминг, LLM-decision | `NYAAI_LLM_MODEL` |
| `jev-style` | Jev-Style: choice / bool / score | `NYAAI_JEVSTYLE_MODEL` |
| `jev-omni` | Jev-Omni: текст + изображение (png/webp) + аудио + видео | `NYAAI_JEVOMNI_MODEL`, `NYAAI_JEVOMNI_HEAD` |

Если параметр не задан — соответствующий пример просто пропускается.

## Запуск

```powershell
# все примеры
dotnet run --project NyaAI.Examples

# конкретный пример
dotnet run --project NyaAI.Examples -- jev-omni
dotnet run --project NyaAI.Examples -- generation
dotnet run --project NyaAI.Examples -- jev-style
```

## Настройка

Параметры задаются аргументами `--key=value` или переменными окружения `NYAAI_*`.
Если задан `NYAAI_SERVER_EXE`, примеры сами поднимают `llama-server` (и гасят его
после завершения).

| Аргумент | Переменная | Описание |
|---|---|---|
| `--base-url=` | `NYAAI_BASE_URL` | адрес сервера (по умолчанию `http://127.0.0.1:8001`) |
| `--api-key=` | `NYAAI_API_KEY` | API-ключ |
| `--llm-model=` | `NYAAI_LLM_MODEL` | GGUF обычной LLM |
| `--llm-alias=` | `NYAAI_LLM_ALIAS` | alias обычной LLM |
| `--jevstyle-model=` | `NYAAI_JEVSTYLE_MODEL` | GGUF Jev-Style |
| `--jevomni-model=` | `NYAAI_JEVOMNI_MODEL` | GGUF Jev-Omni |
| `--jevomni-head=` | `NYAAI_JEVOMNI_HEAD` | `decision-head-f32.npz` |
| `--jevomni-mmproj=` | `NYAAI_JEVOMNI_MMPROJ` | mmproj (иначе ищется рядом с моделью) |
| `--ffmpeg=` | `NYAAI_FFMPEG` | путь к `ffmpeg.exe` |
| `--server-exe=` | `NYAAI_SERVER_EXE` | `llama-server.exe` для автозапуска |

Пример полного прогона:

```powershell
$env:NYAAI_SERVER_EXE    = "F:\SOFT\llama.cpp\llama-server.exe"
$env:NYAAI_LLM_MODEL     = "F:\AI\lmstudio-community\Qwen2.5-Coder-14B-Instruct-GGUF\Qwen2.5-Coder-14B-Instruct-Q4_K_M.gguf"
$env:NYAAI_LLM_ALIAS     = "local/qwen2.5-coder-14b"
$env:NYAAI_JEVSTYLE_MODEL= "F:\AI\Jev-Style-v2-Calibrated-BF16.gguf"
$env:NYAAI_JEVOMNI_MODEL = "F:\AI\Jev-Omni-Unified-Q4_K_M.gguf"
$env:NYAAI_JEVOMNI_HEAD  = "F:\AI\Jev\decision-head-f32.npz"
$env:NYAAI_FFMPEG        = "G:\Dev\ffmpeg-2023-12-14-git-5256b2fbe6-essentials_build\bin\ffmpeg.exe"

dotnet run --project NyaAI.Examples
```

Ожидаемые строки (пример):

```
[completion] Paris. ...
[chat] A mutex is a synchronization mechanism ...
[stream] 1, 2, 3, 4, 5
[decision] best=positive p=0.99
[choice] positive p=0.9966
[text ] started=no yes=0.428
[image] green p=0.923
[webp ] green p=0.926 (перекодировано в PNG)
[audio] steady=no yes=0.142
[video] green p=0.968 (15 кадров)
```

> Примеры запускают по очереди разные модели, но используют один порт. Поэтому
> `ServerBootstrap` переиспользует уже поднятый сервер, только если он здоров;
> если подряд запускать разные примеры на одном сервере — остановите сервер между
> ними (в полном прогоне каждый пример поднимает свой сервер сам).

## Ассеты

Папка `Assets/` содержит простые файлы, по которым проходят нейронки:

| Файл | Описание | Вопрос в примере |
|---|---|---|
| `shape_red.png` / `.webp` | светлый фон, большой красный круг | «какого цвета фигура?» |
| `traffic_light.png` | тёмный фон, жёлтый круг | — |
| `tone_440.mp3` | ровный тон 440 Гц, 2 с | «это ровный тон?» |
| `two_tones.wav` | 440 Гц затем 880 Гц | — |
| `clip_redbox.mp4` | синий фон, красный квадрат | «какого цвета объект?» |

Ассеты не хранятся в git — генерируются скриптом (нужны Pillow и ffmpeg в `PATH`):

```powershell
python NyaAI.Examples\tools\make-assets.py
```

Скрипт создаёт те же файлы, что используются примерами.
