using System.Diagnostics;

namespace NyaAI.Examples;

/// <summary>
/// Опциональный автозапуск llama-server для примеров (если задан ServerExe).
/// Не входит в NyaAI — там сознательно нет управления процессом.
/// </summary>
public sealed class ServerBootstrap : IDisposable
{
    private readonly Process? _process;

    private ServerBootstrap(Process? process) => _process = process;

    public static async Task<ServerBootstrap> EnsureAsync(
        ExampleConfig cfg, IEnumerable<string> serverArgs, CancellationToken cancellationToken = default)
    {
        if (await IsHealthyAsync(cfg, cancellationToken))
            return new ServerBootstrap(null);

        if (string.IsNullOrWhiteSpace(cfg.ServerExe))
            return new ServerBootstrap(null);

        var psi = new ProcessStartInfo
        {
            FileName = cfg.ServerExe,
            WorkingDirectory = Path.GetDirectoryName(cfg.ServerExe) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in serverArgs) psi.ArgumentList.Add(a);

        var proc = Process.Start(psi)!;
        for (int i = 0; i < 180; i++)
        {
            if (proc.HasExited)
                throw new InvalidOperationException($"llama-server завершился с кодом {proc.ExitCode}.");
            if (await IsHealthyAsync(cfg, cancellationToken)) break;
            await Task.Delay(1000, cancellationToken);
        }

        return new ServerBootstrap(proc);
    }

    private static async Task<bool> IsHealthyAsync(ExampleConfig cfg, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        if (!string.IsNullOrEmpty(cfg.ApiKey))
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        try
        {
            using var resp = await http.GetAsync($"{cfg.BaseUrl.TrimEnd('/')}/health", cancellationToken);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_process is null) return;
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch { /* ignore */ }
        _process.Dispose();
    }
}
