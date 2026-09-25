using System.Text.Json;
using QwenLauncher.Core.Runners;

namespace QwenLauncher.Core;

/// <summary>Формирует JSON-сниппет провайдера для opencode.json по профилю.</summary>
public static class OpencodeProvider
{
    public const string ProviderId = "llama.cpp";

    public static string BuildSnippet(LaunchProfile p)
    {
        var runner = RunnerRegistry.Get(p.Runner);

        var modelId = !string.IsNullOrWhiteSpace(p.Alias)
            ? p.Alias
            : Path.GetFileNameWithoutExtension(p.ModelPath);

        var snippet = new
        {
            provider = new Dictionary<string, object>
            {
                [ProviderId] = new
                {
                    npm = "@ai-sdk/openai-compatible",
                    name = "llama.cpp (local)",
                    options = new
                    {
                        baseURL = $"{runner.BaseUrl(p)}/v1",
                        apiKey = p.ApiKey
                    },
                    models = new Dictionary<string, object>
                    {
                        [modelId] = new
                        {
                            name = modelId + " (local)",
                            limit = new { context = p.ContextSize, output = 8192 }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(snippet, new JsonSerializerOptions { WriteIndented = true });
    }
}
