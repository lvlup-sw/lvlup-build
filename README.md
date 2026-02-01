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

## GitHub Packages

This package is published to GitHub Packages. To consume it, add a `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/lvlup-sw/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="GITHUB_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

## License

MIT
