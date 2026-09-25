namespace NyaLlamaLauncher.Core.Runners;

public sealed class LlamaCppRunner : IModelRunner
{
    public string Id => RunnerIds.LlamaCpp;
    public string DisplayName => "llama.cpp (GGUF)";

    public IReadOnlyList<string> BuildArguments(LaunchProfile c)
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

        a.AddRange(ArgTokenizer.Split(c.ExtraArgs));

        return a;
    }

    public string ResolveExecutable(LaunchProfile c) => c.ServerExe;

    public string ResolveWorkingDirectory(LaunchProfile c) =>
        !string.IsNullOrWhiteSpace(c.WorkingDir)
            ? c.WorkingDir
            : Path.GetDirectoryName(c.ServerExe) ?? AppContext.BaseDirectory;

    public IReadOnlyList<string> Validate(LaunchProfile c)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(c.ServerExe) || !File.Exists(c.ServerExe))
            errors.Add("Не найден llama-server.exe: " + c.ServerExe);

        if (string.IsNullOrWhiteSpace(c.ModelPath) || !File.Exists(c.ModelPath))
            errors.Add("Не найден файл модели: " + c.ModelPath);

        if (c.Port is < 1 or > 65535)
            errors.Add("Некорректный порт: " + c.Port);

        if (!string.IsNullOrWhiteSpace(c.MmprojPath) && !File.Exists(c.MmprojPath))
            errors.Add("Не найден mmproj: " + c.MmprojPath);

        if (!string.IsNullOrWhiteSpace(c.TemplatePath) && !File.Exists(c.TemplatePath))
            errors.Add("Не найден шаблон чата: " + c.TemplatePath);

        return errors;
    }

    public string BaseUrl(LaunchProfile c)
    {
        var host = c.Host;
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";
        return $"http://{host}:{c.Port}";
    }

    public string HealthUrl(LaunchProfile c) => BaseUrl(c) + "/health";
}
