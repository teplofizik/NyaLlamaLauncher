namespace QwenLauncher.Core.Runners;

/// <summary>
/// Реестр доступных движков. Чтобы добавить новый тип нейронки — реализуйте
/// <see cref="IModelRunner"/> и добавьте экземпляр в список ниже.
/// </summary>
public static class RunnerRegistry
{
    private static readonly List<IModelRunner> Runners = new()
    {
        new LlamaCppRunner(),
        new CommandRunner(),
    };

    public static IReadOnlyList<IModelRunner> All => Runners;

    public static IModelRunner Default => Runners[0];

    public static IModelRunner Get(string? id)
    {
        return Runners.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? Default;
    }
}
