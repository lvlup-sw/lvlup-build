using System.Reflection.PortableExecutable;
using Lvlup.Build.ConfigureAwait;

try
{
    var assembly = Path.GetFullPath(args[0]);
    using var resolver = new ReferenceResolver(File.ReadLines(args[1]));
    var symbolFormat = GetSymbolFormat(assembly);
    var pdb = Path.ChangeExtension(assembly, ".pdb");
    var embedded = symbolFormat == DebugDirectoryEntryType.EmbeddedPortablePdb;
    var external = !embedded && File.Exists(pdb);

    using var module = ModuleDefinition.ReadModule(assembly, new ReaderParameters
    {
        AssemblyResolver = resolver,
        InMemory = true,
        ReadSymbols = embedded || external,
    });
    if (!new AwaitRewriter(module).Execute())
        return 0;

    var writer = new WriterParameters
    {
        WriteSymbols = embedded || external,
        SymbolWriterProvider = embedded ? new EmbeddedPortablePdbWriterProvider() : new PortablePdbWriterProvider(),
        DeterministicMvid = true,
    };
    if (module.Assembly.Name.HasPublicKey)
    {
        if (args.Length < 3 || !File.Exists(args[2]) || args.Skip(3).Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("ConfigureAwait rewriting requires a full signing key for signed assemblies. Set LvlupConfigureAwait=false for public- or delay-signed builds.");

        writer.StrongNameKeyBlob = File.ReadAllBytes(args[2]);
    }

    // Keep the original filename for the CodeView record.
    var staging = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(assembly)!, $".lvlup-await-{Guid.NewGuid():N}"));
    try
    {
        var rewritten = Path.Combine(staging.FullName, Path.GetFileName(assembly));
        module.Write(rewritten, writer);
        if (external)
            File.Move(Path.ChangeExtension(rewritten, ".pdb"), pdb, overwrite: true);
        File.Move(rewritten, assembly, overwrite: true);
    }
    finally
    {
        staging.Delete(recursive: true);
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"LVLUP002: {exception}");
    return 1;
}

static DebugDirectoryEntryType GetSymbolFormat(string assembly)
{
    using var stream = File.OpenRead(assembly);
    using var reader = new PEReader(stream);
    return reader.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
        ? DebugDirectoryEntryType.EmbeddedPortablePdb
        : DebugDirectoryEntryType.Unknown;
}
