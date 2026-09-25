using System.Text;

namespace QwenLauncher.Core;

public static class ArgTokenizer
{
    public static string Quote(string arg)
    {
        return arg.Contains(' ') || arg.Contains('"')
            ? "\"" + arg.Replace("\"", "\\\"") + "\""
            : arg;
    }

    public static string JoinCommandLine(string exe, IEnumerable<string> args)
    {
        var sb = new StringBuilder();
        sb.Append(Quote(exe));
        foreach (var arg in args)
        {
            sb.Append(' ').Append(Quote(arg));
        }
        return sb.ToString();
    }

    /// <summary>Разбить строку на аргументы, учитывая двойные кавычки.</summary>
    public static IEnumerable<string> Split(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) yield break;

        var sb = new StringBuilder();
        bool inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(ch);
            }
        }

        if (sb.Length > 0) yield return sb.ToString();
    }
}
