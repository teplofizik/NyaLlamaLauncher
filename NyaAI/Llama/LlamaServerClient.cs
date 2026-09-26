using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NyaAI.Decision;

namespace NyaAI.Llama;

/// <summary>
/// Тонкий клиент к llama.cpp server: native <c>/completion</c> (с распределением
/// следующего токена), а также <c>/health</c> и <c>/v1/models</c>.
/// </summary>
/// <remarks>
/// Класс не управляет процессом сервера — предполагается, что он уже запущен
/// (например, профилем в NyaLlamaLauncher).
/// </remarks>
/// <example>
/// <code>
/// using var client = new LlamaServerClient(new LlamaServerOptions
/// {
///     BaseUrl = "http://127.0.0.1:8001",
///     ApiKey  = "..."
/// });
/// if (await client.HealthAsync()) { /* ... */ }
/// </code>
/// </example>
public sealed class LlamaServerClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly LlamaServerOptions _options;

    /// <summary>Создать клиент.</summary>
    /// <param name="options">Настройки подключения.</param>
    /// <param name="httpClient">Внешний <see cref="HttpClient"/> (не будет уничтожен клиентом).</param>
    public LlamaServerClient(LlamaServerOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        BaseUrl = options.BaseUrl.TrimEnd('/');

        if (httpClient is null)
        {
            _http = new HttpClient { Timeout = options.Timeout };
            _ownsHttp = true;
        }
        else
        {
            _http = httpClient;
            _ownsHttp = false;
        }
    }

    /// <summary>Базовый адрес сервера (без завершающего «/»).</summary>
    public string BaseUrl { get; }

    internal HttpClient Http => _http;
    internal string? ApiKey => _options.ApiKey;
    internal LlamaServerOptions Options => _options;

    /// <summary>Проверить доступность сервера (<c>/health</c>).</summary>
    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var resp = await _http.GetAsync(BaseUrl + "/health", cancellationToken);
            return resp.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Список доступных идентификаторов моделей (<c>/v1/models</c>).</summary>
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _http.GetAsync(BaseUrl + "/v1/models", cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new NyaAIException($"GET /v1/models вернул {(int)resp.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        var ids = new List<string>();
        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    ids.Add(id.GetString()!);
            }
        }
        return ids;
    }

    /// <summary>Один жадный шаг генерации и распределение следующего токена (top-N).</summary>
    /// <param name="prompt">Промпт.</param>
    /// <param name="topLogprobs">Сколько top-логвероятностей запросить (<c>n_probs</c>).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<CompletionResult> CompleteAsync(
        string prompt,
        int topLogprobs = 100,
        CancellationToken cancellationToken = default)
    {
        var request = new CompletionRequest
        {
            Prompt = prompt,
            NProbs = Math.Max(1, topLogprobs)
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/completion");
        if (!string.IsNullOrEmpty(_options.ApiKey))
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = JsonSerializer.Serialize(request, RequestJsonOptions);
        msg.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(msg, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            throw new NyaAIException($"POST /completion вернул {(int)resp.StatusCode}: {json}");

        return CompletionResult.Parse(json);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private sealed class CompletionRequest
    {
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = "";
        [JsonPropertyName("n_predict")] public int NPredict { get; set; } = 1;
        [JsonPropertyName("temperature")] public float Temperature { get; set; } = 0f;
        [JsonPropertyName("top_k")] public int TopK { get; set; } = 0;
        [JsonPropertyName("top_p")] public float TopP { get; set; } = 1f;
        [JsonPropertyName("min_p")] public float MinP { get; set; } = 0f;
        [JsonPropertyName("n_probs")] public int NProbs { get; set; } = 100;
        [JsonPropertyName("post_sampling_probs")] public bool PostSamplingProbs { get; set; } = false;
        [JsonPropertyName("cache_prompt")] public bool CachePrompt { get; set; } = true;
        [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
    }
}
