using NyaAI.Examples;

var cfg = ExampleConfig.FromArgs(args);

var examples = new Dictionary<string, Func<ExampleConfig, Task>>(StringComparer.OrdinalIgnoreCase)
{
    ["generation"] = GenerationExample.RunAsync,
    ["jev-style"] = JevStyleExample.RunAsync,
    ["jev-omni"] = JevOmniExample.RunAsync,
};

if (args.Length > 0 && !args[0].StartsWith("--"))
{
    var name = args[0];
    if (!examples.TryGetValue(name, out var fn))
    {
        Console.WriteLine($"Неизвестный пример '{name}'. Доступны: {string.Join(", ", examples.Keys)}");
        return 1;
    }
    await fn(cfg);
    return 0;
}

Console.WriteLine("NyaAI.Examples");
Console.WriteLine($"  base-url   : {cfg.BaseUrl}");
Console.WriteLine($"  llm-model  : {cfg.LlmModel ?? "(не задан)"}");
Console.WriteLine($"  jevstyle   : {cfg.JevStyleModel ?? "(не задан)"}");
Console.WriteLine($"  jevomni    : {cfg.JevOmniModel ?? "(не задан)"}");
Console.WriteLine($"  assets     : {ExampleConfig.AssetsDir}");
Console.WriteLine();

foreach (var (name, fn) in examples)
{
    Console.WriteLine($"===== {name} =====");
    try
    {
        await fn(cfg);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [пропущено/ошибка] {ex.Message}");
    }
    Console.WriteLine();
}

return 0;
