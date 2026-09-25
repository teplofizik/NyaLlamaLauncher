# NyaLlama Launcher

GUI-лаунчер для локального запуска GGUF-моделей через `llama.cpp` (`llama-server`)
на `127.0.0.1`, с профилями под разные нейронки и подключаемыми движками (runners).

- Приложение: `Launcher\NyaLlamaLauncher.exe` (ярлык «NyaLlama Launcher» на рабочем столе)
- Конфиг: `Launcher\config.yaml` (список профилей + выбранный профиль)
- Логика: `NyaLlamaLauncher\Core` (профили, раннеры, процесс) — GUI: `NyaLlamaLauncher\UI`
- Сервер: `F:\SOFT\llama.cpp\llama-server.exe`
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
| Jev-Style-2B Decision (Q8_0) | Jev-Style-v2-Calibrated-Q8_0 | 2.0 ГБ | да | [chaoliangUNSW/Jev-Style-Qwen3.5-2B-Decision-v2-GGUF](https://huggingface.co/chaoliangUNSW/Jev-Style-Qwen3.5-2B-Decision-v2-GGUF) |
| Jev-Omni Q4_K_M (мультимодал) | Jev-Omni-Unified-Q4_K_M + mmproj | 7.4 ГБ | да (~9 ГиБ VRAM) | [Reza2kn/Jev-Omni-Q4_K_M-GGUF](https://huggingface.co/Reza2kn/Jev-Omni-Q4_K_M-GGUF) |
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
