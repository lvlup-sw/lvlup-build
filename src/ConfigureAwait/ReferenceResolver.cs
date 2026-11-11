namespace Lvlup.Build.ConfigureAwait;

internal sealed class ReferenceResolver : DefaultAssemblyResolver
{
    private readonly Dictionary<string, string> referencePaths;

    public ReferenceResolver(IEnumerable<string> paths)
    {
        referencePaths = paths.GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        if (!referencePaths.TryGetValue(name.Name, out var path))
            return base.Resolve(name, parameters);

        parameters.AssemblyResolver = this;
        var assembly = AssemblyDefinition.ReadAssembly(path, parameters);
        RegisterAssembly(assembly);
        return assembly;
    }
}
