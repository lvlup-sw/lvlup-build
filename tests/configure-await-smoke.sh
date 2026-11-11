#!/usr/bin/env bash
set -euo pipefail

feed="$(realpath "${1:?pass the directory containing the packed Lvlup.Build}")"
version="${2:?pass the packed package version}"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT
cp "$(dirname "$0")"/ConfigureAwait/* "$fixture/"
cat > "$fixture/Directory.Packages.props" <<XML
<Project>
  <ItemGroup>
    <PackageVersion Include="Lvlup.Build" Version="$version" />
  </ItemGroup>
</Project>
XML

cd "$fixture"
dotnet restore -s "$feed" -s https://api.nuget.org/v3/index.json
for configuration in Debug Release; do
  dotnet run --no-restore -c "$configuration"
  dotnet run --no-restore -c "$configuration"
  dotnet run --no-restore -c "$configuration" -p:LvlupConfigureAwait=false -- opt-out
  dotnet run --no-restore -c "$configuration"
done
