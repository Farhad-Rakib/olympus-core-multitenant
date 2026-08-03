using System.Reflection;

namespace ProjectNamePlaceholder.Application.ModuleSystem;

public sealed class ModuleHost
{
    // Assembly scanning, not a config file or attribute -- a module only needs to implement
    // IOlympusModule and be a concrete type in one of the scanned assemblies to be picked up.
    public IReadOnlyList<IOlympusModule> DiscoverModules(IEnumerable<Assembly> assemblies)
    {
        var modules = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IOlympusModule).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
            .Select(type => (IOlympusModule)Activator.CreateInstance(type)!)
            .ToList();

        ValidateDependencies(modules);
        return TopologicalSort(modules);
    }

    private static void ValidateDependencies(IReadOnlyList<IOlympusModule> modules)
    {
        var keys = modules.Select(m => m.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            foreach (var dependency in module.Dependencies)
            {
                if (!keys.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Key}' requires '{dependency}', which was not discovered. " +
                        "Optional dependencies belong in OptionalDependencies instead, not Dependencies.");
                }
            }
        }
    }

    // Required dependencies load before their dependents; optional dependencies influence order
    // when present but never block discovery when absent.
    private static List<IOlympusModule> TopologicalSort(IReadOnlyList<IOlympusModule> modules)
    {
        var byKey = modules.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);
        var sorted = new List<IOlympusModule>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(IOlympusModule module)
        {
            if (visited.Contains(module.Key))
            {
                return;
            }

            if (!visiting.Add(module.Key))
            {
                throw new InvalidOperationException($"Circular module dependency detected involving '{module.Key}'.");
            }

            foreach (var depKey in module.Dependencies.Concat(module.OptionalDependencies))
            {
                if (byKey.TryGetValue(depKey, out var dependency))
                {
                    Visit(dependency);
                }
            }

            visiting.Remove(module.Key);
            visited.Add(module.Key);
            sorted.Add(module);
        }

        foreach (var module in modules)
        {
            Visit(module);
        }

        return sorted;
    }
}
