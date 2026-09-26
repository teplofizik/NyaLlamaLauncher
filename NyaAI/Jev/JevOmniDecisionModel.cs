using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NyaAI.Decision;
using NyaAI.Llama;

namespace NyaAI.Jev;

/// <summary>
/// Реализация <see cref="IDecisionModel"/> для Jev-Omni (Gemma 4 12B):
/// сервер запускается с <c>--embedding --pooling none</c> (+ mmproj для медиа),
/// берётся последний hidden-вектор и к нему применяется FP32 decision-head.
/// Медиа передаётся через <see cref="DecisionRequest.Media"/>.
/// </summary>
public sealed class JevOmniDecisionModel : IDecisionModel
{
    private readonly LlamaServerClient _client;
    private readonly JevOmniHead _head;
    private string? _mediaMarker;

    public JevOmniDecisionModel(LlamaServerClient client, string decisionHeadPath)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _head = JevOmniHead.Load(decisionHeadPath);
    }

    public async Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        var count = request.Options.Count;
        if (count is < 2 or > JevOmniHead.MaxOptions)
            throw new ArgumentException($"Число вариантов должно быть 2..{JevOmniHead.MaxOptions}.", nameof(request));

        var text = BuildDecisionText(request);
        float[] hidden;
        string mediaInfo = "";

        if (request.Media is null || request.Media.Count == 0)
        {
            var prompt = $"<|turn>user\n{text}<turn|>\n<|turn>model\n<|channel>thought\n<|channel|>";
            hidden = await PostForHiddenAsync("/embeddings", new { input = prompt }, cancellationToken);
        }
        else
        {
            var marker = await GetMediaMarkerAsync(cancellationToken);
            mediaInfo = $" media={request.Media.Count}";
            var prompt = $"<|turn>user\n{string.Concat(Enumerable.Repeat(marker, request.Media.Count))}{text}<turn|>\n" +
                         "<|turn>model\n<|channel>thought\n<|channel|>";
            var media = request.Media.Select(m => Convert.ToBase64String(m.Data)).ToArray();
            var payload = new
            {
                content = new { prompt_string = prompt, multimodal_data = media },
                embd_normalize = -1
            };
            hidden = await PostForHiddenAsync("/embedding", payload, cancellationToken);
        }

        var z = _head.Logits(hidden, count);
        var probabilities = Softmax(z);

        var scored = new List<ScoredOption>(count);
        for (int i = 0; i < count; i++)
            scored.Add(new ScoredOption(i, request.Options[i], probabilities[i], Math.Log(Math.Max(probabilities[i], 1e-30))));

        var ranked = scored.OrderByDescending(s => s.Probability).ToList();

        double? yesProbability = null;
        double? expectedScore = null;

        if (request.Kind == DecisionKind.Bool)
        {
            var idx = IndexOfYes(request.Options);
            yesProbability = probabilities[idx];
        }
        else if (request.Kind == DecisionKind.Score)
        {
            double expected = 0;
            for (int i = 0; i < count; i++) expected += i * probabilities[i];
            expectedScore = expected;
        }

        return new DecisionResult
        {
            Options = ranked,
            YesProbability = yesProbability,
            ExpectedScore = expectedScore,
            RawContent = mediaInfo + " z=[" + string.Join(", ", z.Select(v => v.ToString("0.00"))) + "]"
        };
    }

    private static string BuildDecisionText(DecisionRequest request)
    {
        var choices = string.Join("\n", request.Options.Select((o, i) => $"{i + 1}. {o}"));
        return $"{request.State}\n\n---\n\nQUESTION: {request.Question}\n\nOPTIONS:\n{choices}\n\n" +
               $"Reply with only the number of the correct option (1-{request.Options.Count}).\n" +
               "Output a single number and nothing else.";
    }

    private async Task<string> GetMediaMarkerAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_mediaMarker)) return _mediaMarker!;

        using var req = new HttpRequestMessage(HttpMethod.Get, _client.BaseUrl + "/props");
        if (!string.IsNullOrEmpty(_client.ApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _client.ApiKey);

        using var resp = await _client.Http.SendAsync(req, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new NyaAIException($"GET /props вернул {(int)resp.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("media_marker", out var m) || m.ValueKind != JsonValueKind.String)
            throw new NyaAIException("Сервер не вернул media_marker (мультимодальность не активна?).");

        _mediaMarker = m.GetString()!;
        return _mediaMarker;
    }

    private async Task<float[]> PostForHiddenAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, _client.BaseUrl + path);
        if (!string.IsNullOrEmpty(_client.ApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _client.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await _client.Http.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new NyaAIException($"POST {path} вернул {(int)resp.StatusCode}: {json}");

        return ExtractLastHidden(json);
    }

    internal static float[] ExtractLastHidden(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        JsonElement item = root;
        if (root.ValueKind == JsonValueKind.Array)
        {
            if (root.GetArrayLength() == 0) throw new NyaAIException("Пустой ответ эмбеддингов.");
            item = root[0];
        }
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) &&
                 data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
        {
            item = data[0];
        }

        if (!item.TryGetProperty("embedding", out var emb) || emb.ValueKind != JsonValueKind.Array)
            throw new NyaAIException("В ответе нет поля embedding.");

        // emb: либо список векторов по токенам, либо один плоский вектор
        JsonElement vector = emb;
        if (emb.GetArrayLength() > 0 && emb[0].ValueKind == JsonValueKind.Array)
            vector = emb[emb.GetArrayLength() - 1]; // последний токен

        var result = new float[vector.GetArrayLength()];
        int i = 0;
        foreach (var v in vector.EnumerateArray())
            result[i++] = (float)v.GetDouble();

        return result;
    }

    private static double[] Softmax(double[] z)
    {
        var max = z.Max();
        var result = new double[z.Length];
        double sum = 0;
        for (int i = 0; i < z.Length; i++)
        {
            result[i] = Math.Exp(z[i] - max);
            sum += result[i];
        }
        for (int i = 0; i < z.Length; i++) result[i] /= sum;
        return result;
    }

    private static int IndexOfYes(IReadOnlyList<string> options)
    {
        for (int i = 0; i < options.Count; i++)
            if (string.Equals(options[i].Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }
}
