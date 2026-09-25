using System.Diagnostics;
using System.Text;

namespace QwenLauncher.Core;

/// <summary>
/// Управление процессом сервера: запуск, остановка, стриминг stdout/stderr.
/// Ничего не знает о конкретном движке — принимает готовые exe/каталог/аргументы.
/// </summary>
public sealed class ServerProcess : IDisposable
{
    private Process? _proc;

    public event Action<string>? Output;
    public event Action? Exited;

    public bool IsRunning => _proc is { HasExited: false };
    public int Pid => _proc is { HasExited: false } ? _proc.Id : -1;

    public void Start(string fileName, string workingDirectory, IReadOnlyList<string> args)
    {
        if (IsRunning) throw new InvalidOperationException("Сервер уже запущен.");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args) psi.ArgumentList.Add(arg);

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
            // процесс уже завершён
        }
    }

    public void Dispose()
    {
        Stop();
        _proc?.Dispose();
        _proc = null;
    }
}
