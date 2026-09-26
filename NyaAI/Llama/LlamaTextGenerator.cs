using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NyaAI.Decision;
using NyaAI.Generation;

namespace NyaAI.Llama;

/// <summary>
/// Обычная LLM через llama.cpp: низкий уровень (native <c>/completion</c>) и
/// чат (<c>/v1/chat/completions</c>), с блокирующим и потоковым режимами.
/// </summary>
/// <example>
/// <code>
/// var llm = new LlamaTextGenerator(client);
/// var chat = await llm.ChatAsync(new[]
/// {
///     ChatMessage.System("Отвечай кратко."),
///     ChatMessage.User("Что такое мьютекс?")
/// });
/// </code>
/// </example>
public sealed class LlamaTextGenerator : ITextGenerator
{
    private readonly LlamaServerClient _client;

    /// <summary>Создать генератор поверх клиента.</summary>
    /// <param name="client">Клиент llama.cpp server.</param>
    public LlamaTextGenerator(LlamaServerClient client) =>
        _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<GenerationResult> GenerateAsync(
        string prompt,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var dto = CompletionDto.Build(prompt ?? "", Merge(options), stream: false);
        var json = await PostAsync("/completion", dto, cancellationToken);
        return ParseCompletion(json);
    }

    /// <inheritdoc />
    public async Task<GenerationResult> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var dto = ChatDto.Build(_client.Options.Model, messages, Merge(options), stream: false);
        var json = await PostAsync("/v1/chat/completions", dto, cancellationToken);
        return ParseChat(json);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        GenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dto = CompletionDto.Build(prompt ?? "", Merge(options), stream: true);
        await foreach (var chunk in StreamAsync("/completion", dto, completionMode: true, cancellationToken))
            yield return chunk;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamAsync(
        IReadOnlyList<ChatMessage> messages,
        GenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dto = ChatDto.Build(_client.Options.Model, messages, Merge(options), stream: true);
        await foreach (var chunk in StreamAsync("/v1/chat/completions", dto, completionMode: false, cancellationToken))
            yield return chunk;
    }

    private GenerationOptions Merge(GenerationOptions? options) =>
        options ?? _client.Options.DefaultGeneration;

    private async Task<string> PostAsync(string path, object dto, CancellationToken cancellationToken)
    {
        using var req = BuildRequest(path, dto);
        using var resp = await _client.Http.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new NyaAIException($"POST {path} вернул {(int)resp.StatusCode}: {json}");
        return json;
    }

    private async IAsyncEnumerable<string> StreamAsync(
        string path,
        object dto,
        bool completionMode,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var req = BuildRequest(path, dto);
        using var resp = await _client.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new NyaAIException($"POST {path} (stream) вернул {(int)resp.StatusCode}: {err}");
        }

        using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (line.Length == 0) continue;
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var data = line.Substring(5).Trim();
            if (data == "[DONE]") break;

            var token = completionMode ? ExtractCompletionContent(data) : ExtractChatDelta(data);
            if (!string.IsNullOrEmpty(token)) yield return token;
        }
    }

    private HttpRequestMessage BuildRequest(string path, object dto)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, _client.BaseUrl + path);
        if (!string.IsNullOrEmpty(_client.ApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _client.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(dto, JsonOpts), Encoding.UTF8, "application/json");
        return req;
    }

    private static GenerationResult ParseCompletion(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        var stop = root.TryGetProperty("stop_type", out var st) ? st.GetString() ?? "" : "";
        var gen = root.TryGetProperty("tokens_predicted", out var tp) && tp.TryGetInt32(out var g) ? g : 0;
        var eval = root.TryGetProperty("tokens_evaluated", out var te) && te.TryGetInt32(out var e) ? e : 0;

        double pps = 0, gps = 0;
        if (root.TryGetProperty("timings", out var t))
        {
            if (t.TryGetProperty("prompt_per_second", out var p) && p.TryGetDouble(out var pv)) pps = pv;
            if (t.TryGetProperty("predicted_per_second", out var g2) && g2.TryGetDouble(out var gv)) gps = gv;
        }

        return new GenerationResult
        {
            Text = content,
            StopReason = stop,
            GeneratedTokens = gen,
            PromptTokens = eval,
            PromptTokensPerSecond = pps,
            GenerationTokensPerSecond = gps
        };
    }

    private static GenerationResult ParseChat(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var text = "";
        var finish = "";
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var cont))
                text = cont.GetString() ?? "";
            if (choice.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String)
                finish = fr.GetString() ?? "";
        }

        int prompt = 0, completion = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var pv)) prompt = pv;
            if (usage.TryGetProperty("completion_tokens", out var ct) && ct.TryGetInt32(out var cv)) completion = cv;
        }

        double pps = 0, gps = 0;
        if (root.TryGetProperty("timings", out var t))
        {
            if (t.TryGetProperty("prompt_per_second", out var p) && p.TryGetDouble(out var pvv)) pps = pvv;
            if (t.TryGetProperty("predicted_per_second", out var g2) && g2.TryGetDouble(out var gvv)) gps = gvv;
        }

        return new GenerationResult
        {
            Text = text,
            StopReason = finish,
            GeneratedTokens = completion,
            PromptTokens = prompt,
            PromptTokensPerSecond = pps,
            GenerationTokensPerSecond = gps
        };
    }

    private static string? ExtractCompletionContent(string data)
    {
        using var doc = JsonDocument.Parse(data);
        return doc.RootElement.TryGetProperty("content", out var c) ? c.GetString() : null;
    }

    private static string? ExtractChatDelta(string data)
    {
        using var doc = JsonDocument.Parse(data);
        if (doc.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }
        return null;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class CompletionDto
    {
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = "";
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("n_predict")] public int? NPredict { get; set; }
        [JsonPropertyName("temperature")] public double? Temperature { get; set; }
        [JsonPropertyName("top_p")] public double? TopP { get; set; }
        [JsonPropertyName("top_k")] public int? TopK { get; set; }
        [JsonPropertyName("min_p")] public double? MinP { get; set; }
        [JsonPropertyName("stop")] public IReadOnlyList<string>? Stop { get; set; }
        [JsonPropertyName("seed")] public uint? Seed { get; set; }
        [JsonPropertyName("cache_prompt")] public bool CachePrompt { get; set; } = true;

        public static CompletionDto Build(string prompt, GenerationOptions o, bool stream) => new()
        {
            Prompt = prompt,
            Stream = stream,
            NPredict = o.MaxTokens,
            Temperature = o.Temperature,
            TopP = o.TopP,
            TopK = o.TopK,
            MinP = o.MinP,
            Stop = o.Stop,
            Seed = o.Seed,
            CachePrompt = o.CachePrompt
        };
    }

    private sealed class ChatDto
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "";
        [JsonPropertyName("messages")] public IReadOnlyList<ChatMessageDto> Messages { get; set; } = Array.Empty<ChatMessageDto>();
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
        [JsonPropertyName("temperature")] public double? Temperature { get; set; }
        [JsonPropertyName("top_p")] public double? TopP { get; set; }
        [JsonPropertyName("top_k")] public int? TopK { get; set; }
        [JsonPropertyName("min_p")] public double? MinP { get; set; }
        [JsonPropertyName("stop")] public IReadOnlyList<string>? Stop { get; set; }
        [JsonPropertyName("seed")] public uint? Seed { get; set; }
        [JsonPropertyName("cache_prompt")] public bool CachePrompt { get; set; } = true;

        public static ChatDto Build(string? model, IReadOnlyList<ChatMessage> messages, GenerationOptions o, bool stream) => new()
        {
            Model = model ?? "",
            Messages = messages.Select(m => new ChatMessageDto { Role = m.Role, Content = m.Content }).ToList(),
            Stream = stream,
            MaxTokens = o.MaxTokens,
            Temperature = o.Temperature,
            TopP = o.TopP,
            TopK = o.TopK,
            MinP = o.MinP,
            Stop = o.Stop,
            Seed = o.Seed,
            CachePrompt = o.CachePrompt
        };
    }

    private sealed class ChatMessageDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }
}
