# Lvlup.Build

Levelup Software build standards, analyzers, and code style enforcement for .NET projects.

## Overview

`Lvlup.Build` is a NuGet meta-package that provides:

- **Analyzers**: StyleCop, Microsoft .NET Analyzers, and Visual Studio Threading Analyzers
- **Build Properties**: Consistent settings for nullable reference types, warnings-as-errors, and deterministic builds
- **Override Points**: Easy customization for project-specific needs

## Installation

Add to your `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="Lvlup.Build" PrivateAssets="all" />
</ItemGroup>
```

And to your `Directory.Packages.props`:

```xml
<ItemGroup>
  <PackageVersion Include="Lvlup.Build" Version="1.0.0" />
</ItemGroup>
```

## What's Included

### Analyzers

| Package | Version | Purpose |
|---------|---------|---------|
| StyleCop.Analyzers | 1.2.0-beta.556 | Code style and formatting |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.0 | Code quality and .NET best practices |
| Microsoft.VisualStudio.Threading.Analyzers | 17.14.15 | Async/threading correctness |

### StyleCop Configuration

The package includes a default `stylecop.json` that is automatically linked to your projects. To override:

1. Add your own `stylecop.json` to your `src/` directory
2. Or disable the default: `<LvlupIncludeStyleCopConfig>false</LvlupIncludeStyleCopConfig>`

### Build Properties

The package automatically configures:

- `LangVersion`: `latest`
- `ImplicitUsings`: `enable`
- `Nullable`: `enable`
- `EnableNETAnalyzers`: `true`
- `AnalysisLevel`: `latest`
- `TreatWarningsAsErrors`: `true`
- `Deterministic`: `true`

## Override Points

Customize behavior by setting properties **before** the package is imported:

```xml
<!-- In your Directory.Build.props -->
<PropertyGroup>
  <!-- Disable warnings-as-errors for Debug builds -->
  <LvlupTreatWarningsAsErrors Condition="'$(Configuration)' == 'Debug'">false</LvlupTreatWarningsAsErrors>

  <!-- Disable all analyzers (not recommended) -->
  <LvlupEnableAnalyzers>false</LvlupEnableAnalyzers>

  <!-- Disable nullable reference types -->
  <LvlupEnableNullable>false</LvlupEnableNullable>

  <!-- Don't run analyzers during build (IDE only) -->
  <LvlupRunAnalyzersDuringBuild>false</LvlupRunAnalyzersDuringBuild>
</PropertyGroup>
```

## Companion Configuration

For full code style enforcement, pair this package with:

1. **`.editorconfig`** - Code formatting rules and analyzer severities
2. **`stylecop.json`** - StyleCop-specific settings (company name, documentation rules)

Templates for these files are available via the `/dotnet-standards` Claude skill:

```bash
/dotnet-standards scaffold MyProject --company "My Company"
```

## Installation

This package is published to [nuget.org](https://www.nuget.org/packages/Lvlup.Build). No special configuration needed.

## License

MIT

## Automatic ConfigureAwait

C# builds using the .NET 10 SDK apply `ConfigureAwait(false)` to implicit awaits of
`Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`, including the implicit
awaits emitted for asynchronous iteration and disposal. Explicit ConfigureAwait
calls retain their semantics. The task runs during the build and uses no Fody
packages. The IL tooling is bundled privately in this package.

For a project that needs its previous capture behavior, opt out in the project
file or set the property in the repository's Directory.Build.props:

```xml
<PropertyGroup>
  <LvlupConfigureAwait>false</LvlupConfigureAwait>
</PropertyGroup>
```

Opting out restores the VSTHRD111 source policy. Opted-in projects suppress only
that diagnostic. Review UI and other context-sensitive code for explicit capture
when upgrading. Public- or delay-signed projects should opt out.

The IL transformation is adapted from ConfigureAwait 3.4.1 under MIT. The
original notice is included alongside the packaged build tooling.
