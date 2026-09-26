using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NyaAI.Decision;

namespace NyaAI.Generation;

/// <summary>
/// Универсальный промпт «обычной LLM как decision-модели»: модель выбирает вариант
/// (Choice/Bool) или называет номер (Score), ответ сопоставляется с вариантами
/// (точное совпадение → вхождение → коэффициент Жаккара).
/// </summary>
public sealed class LlmOptionPrompt : ILlmDecisionPrompt
{
    /// <inheritdoc />
    public string System { get; init; } =
        "You are a decision function. Answer with only the chosen option, exactly as written, without explanation.";

    /// <inheritdoc />
    public string BuildUser(DecisionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[State]");
        sb.AppendLine(request.State);
        sb.AppendLine();
        sb.AppendLine("[Question]");
        sb.AppendLine(request.Question);
        sb.AppendLine();
        sb.AppendLine("[Options]");
        for (int i = 0; i < request.Options.Count; i++)
            sb.AppendLine($"{i}. {request.Options[i]}");

        sb.AppendLine();
        sb.Append(request.Kind == DecisionKind.Score
            ? $"Answer with a single number from 0 to {request.Options.Count - 1} (0 = lowest, {request.Options.Count - 1} = highest)."
            : "Answer with exactly one option from the list, verbatim.");

        return sb.ToString();
    }

    /// <inheritdoc />
    public DecisionResult Parse(string output, DecisionRequest request)
    {
        return request.Kind == DecisionKind.Score
            ? ParseScore(output, request)
            : ParseChoice(output, request);
    }

    private static DecisionResult ParseChoice(string output, DecisionRequest request)
    {
        var n = request.Options.Count;
        var scores = new double[n];
        for (int i = 0; i < n; i++)
            scores[i] = Match(output, request.Options[i]);

        double[] probs;
        if (scores.Max() <= 0)
        {
            probs = new double[n];
            for (int i = 0; i < n; i++) probs[i] = 1.0 / n; // не удалось сопоставить — равномерно
        }
        else
        {
            probs = Softmax(scores, 6.0);
        }

        var ranked = BuildRanked(request.Options, probs);

        double? yes = null;
        if (request.Kind == DecisionKind.Bool)
        {
            var idx = IndexOfYes(request.Options);
            yes = probs[idx];
        }

        return new DecisionResult { Options = ranked, YesProbability = yes, RawContent = output };
    }

    private static DecisionResult ParseScore(string output, DecisionRequest request)
    {
        var n = request.Options.Count;
        var num = ExtractNumber(output);

        double value;
        if (num is null)
        {
            var best = 0;
            var bestScore = -1.0;
            for (int i = 0; i < n; i++)
            {
                var s = Match(output, request.Options[i]);
                if (s > bestScore) { bestScore = s; best = i; }
            }
            value = best;
        }
        else
        {
            value = Math.Clamp(num.Value, 0, n - 1);
        }

        var probs = new double[n];
        probs[(int)Math.Round(value)] = 1.0;

        return new DecisionResult
        {
            Options = BuildRanked(request.Options, probs),
            ExpectedScore = value,
            RawContent = output
        };
    }

    private static List<ScoredOption> BuildRanked(IReadOnlyList<string> options, double[] probs)
    {
        var scored = new List<ScoredOption>(options.Count);
        for (int i = 0; i < options.Count; i++)
            scored.Add(new ScoredOption(i, options[i], probs[i], Math.Log(Math.Max(probs[i], 1e-30))));
        return scored.OrderByDescending(s => s.Probability).ToList();
    }

    private static double[] Softmax(double[] values, double k)
    {
        var max = values.Max();
        var exps = new double[values.Length];
        double sum = 0;
        for (int i = 0; i < values.Length; i++)
        {
            exps[i] = Math.Exp(k * (values[i] - max));
            sum += exps[i];
        }
        for (int i = 0; i < values.Length; i++) exps[i] /= sum;
        return exps;
    }

    private static int IndexOfYes(IReadOnlyList<string> options)
    {
        for (int i = 0; i < options.Count; i++)
            if (string.Equals(options[i].Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    private static double? ExtractNumber(string text)
    {
        var m = Regex.Match(text, @"-?\d+(?:[.,]\d+)?");
        if (!m.Success) return null;
        var s = m.Value.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static double Match(string a, string b)
    {
        a = Normalize(a);
        b = Normalize(b);
        if (a.Length == 0 || b.Length == 0) return 0;

        if (a == b) return 1.0;
        if (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal)) return 0.9;
        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal)) return 0.75;

        var j = Jaccard(a, b);
        return j > 0 ? 0.5 * j : 0;
    }

    private static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        s = s.Trim('"', '\'', '`', '.', ',', '!', '?', ':', ';', '*', ' ', '\n', '\r', '\t');
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }

    private static double Jaccard(string a, string b)
    {
        var sa = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var sb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (sa.Count == 0 || sb.Count == 0) return 0;
        var inter = sa.Intersect(sb).Count();
        var union = sa.Union(sb).Count();
        return union == 0 ? 0 : (double)inter / union;
    }
}
