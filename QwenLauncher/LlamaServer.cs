using System.Diagnostics;
using System.Text;

namespace QwenLauncher;

public sealed class LlamaServer : IDisposable
{
    private Process? _proc;

    public event Action<string>? Output;
    public event Action? Exited;

    public bool IsRunning => _proc is { HasExited: false };
    public int Pid => _proc is { HasExited: false } ? _proc.Id : -1;

    public static List<string> BuildArgs(AppConfig c)
    {
        var a = new List<string>
        {
            "--model", c.ModelPath,
        };

        if (!string.IsNullOrWhiteSpace(c.MmprojPath))
        {
            a.Add("--mmproj");
            a.Add(c.MmprojPath);
            a.Add("--no-mmproj-offload");
        }

        if (!string.IsNullOrWhiteSpace(c.Alias))
        {
            a.Add("--alias");
            a.Add(c.Alias);
        }

        a.Add("--jinja");

        if (!string.IsNullOrWhiteSpace(c.TemplatePath))
        {
            a.Add("--chat-template-file");
            a.Add(c.TemplatePath);
        }

        a.Add("--host");
        a.Add(string.IsNullOrWhiteSpace(c.Host) ? "127.0.0.1" : c.Host.Trim());

        a.Add("--port");
        a.Add(c.Port.ToString());

        a.Add("--ctx-size");
        a.Add(c.ContextSize.ToString());

        a.Add("--parallel");
        a.Add("1");

        a.Add("--flash-attn");
        a.Add(c.FlashAttn ? "on" : "off");

        if (!string.IsNullOrWhiteSpace(c.CacheTypeK))
        {
            a.Add("--cache-type-k");
            a.Add(c.CacheTypeK);
        }

        if (!string.IsNullOrWhiteSpace(c.CacheTypeV))
        {
            a.Add("--cache-type-v");
            a.Add(c.CacheTypeV);
        }

        if (int.TryParse(c.GpuLayers, out var ngl))
        {
            a.Add("-ngl");
            a.Add(ngl.ToString());
        }

        if (int.TryParse(c.Threads, out var threads))
        {
            a.Add("-t");
            a.Add(threads.ToString());
        }

        if (c.Reasoning)
        {
            a.Add("--reasoning");
            a.Add("on");
            a.Add("--reasoning-format");
            a.Add("deepseek");
        }

        if (c.ContextShift)
        {
            a.Add("--context-shift");
        }

        a.Add("--metrics");
        a.Add("--timeout");
        a.Add("3600");

        if (!c.WebUi)
        {
            a.Add("--no-ui");
        }

        if (!string.IsNullOrWhiteSpace(c.ApiKey))
        {
            a.Add("--api-key");
            a.Add(c.ApiKey.Trim());
        }

        a.AddRange(SplitArgs(c.ExtraArgs));

        return a;
    }

    public void Start(AppConfig c)
    {
        if (IsRunning) throw new InvalidOperationException("Сервер уже запущен.");
        if (!File.Exists(c.ServerExe)) throw new FileNotFoundException("Не найден llama-server.exe", c.ServerExe);
        if (!File.Exists(c.ModelPath)) throw new FileNotFoundException("Не найден файл модели", c.ModelPath);

        var psi = new ProcessStartInfo
        {
            FileName = c.ServerExe,
            WorkingDirectory = Path.GetDirectoryName(c.ServerExe) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in BuildArgs(c)) psi.ArgumentList.Add(arg);

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) => { if (e.Data is not null) Output?.Invoke(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) Output?.Invoke(e.Data); };
        proc.Exited += (_, _) => Exited?.Invoke();

        if (!proc.Start()) throw new InvalidOperationException("Не удалось запустить процесс.");
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        _proc = proc;
    }

    public void Stop(int waitMs = 8000)
    {
        var proc = _proc;
        if (proc is null) return;
        try
        {
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(waitMs);
            }
        }
        catch
        {
            // process already gone
        }
    }

    public void Dispose()
    {
        Stop();
        _proc?.Dispose();
        _proc = null;
    }

    public static string Quote(string arg)
    {
        return arg.Contains(' ') || arg.Contains('"')
            ? "\"" + arg.Replace("\"", "\\\"") + "\""
            : arg;
    }

    public static string BuildCommandLine(AppConfig c)
    {
        var sb = new StringBuilder();
        sb.Append(Quote(c.ServerExe));
        foreach (var arg in BuildArgs(c))
        {
            sb.Append(' ').Append(Quote(arg));
        }
        return sb.ToString();
    }

    private static IEnumerable<string> SplitArgs(string? line)
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
