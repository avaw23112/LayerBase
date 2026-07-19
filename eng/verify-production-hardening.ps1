$ErrorActionPreference = "Stop"

dotnet restore

dotnet build `
  -c Release `
  --no-restore

dotnet test `
  LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --no-build

dotnet test `
  LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --no-build `
  --filter "TestCategory=ProductionHardening"

dotnet test `
  LayerBase.Test/LayerBase.Test.csproj `
  -c Release `
  --no-build `
  --filter "TestCategory=ProductionSoak"

dotnet list LayerBase/LayerBase.csproj `
  package `
  --vulnerable `
  --include-transitive

dotnet pack LayerBase/LayerBase.csproj `
  -c Release `
  --no-build `
  -o artifacts/packages `
  /p:ContinuousIntegrationBuild=true `
  /p:Deterministic=true

$packages = Get-ChildItem artifacts/packages -Filter *.nupkg

if ($packages.Count -eq 0) {
    throw "No NuGet package was produced."
}

$trackedArtifacts = git ls-files |
    Select-String -Pattern "TestResults|\.trx$"

if ($trackedArtifacts)
{
    Write-Error "Tracked test artifacts detected:`n$trackedArtifacts"
}

$dirty = git status --porcelain

if ($dirty)
{
    Write-Error "Verification changed the worktree:`n$dirty"
}

Write-Host "Production hardening verification passed."
