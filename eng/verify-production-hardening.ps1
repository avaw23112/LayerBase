param(
    [string]$Configuration = "Release",
    [switch]$RunSoak
)

$ErrorActionPreference = "Stop"

Write-Host "=== Verify Generated Assembly Modules ===" -ForegroundColor Cyan
pwsh "$PSScriptRoot\verify-generated-assembly-modules.ps1"

Write-Host "=== Build $Configuration ===" -ForegroundColor Cyan
dotnet build LayerBase.sln --configuration $Configuration --no-restore

Write-Host "=== Run ProductionHardening Tests ===" -ForegroundColor Cyan
dotnet test LayerBase.Test/LayerBase.Test.csproj `
    --configuration $Configuration `
    --no-build `
    --filter "TestCategory=ProductionHardening"

Write-Host "=== Run Generator Tests ===" -ForegroundColor Cyan
dotnet test LayerBase.Generator/LayerBase.Generator.Tests/LayerBase.Generator.Tests.csproj `
    --configuration Release

if ($RunSoak)
{
    Write-Host "=== Run ProductionSoak Tests ===" -ForegroundColor Cyan
    dotnet test LayerBase.Test/LayerBase.Test.csproj `
        --configuration $Configuration `
        --no-build `
        --filter "TestCategory=ProductionSoak"
}

Write-Host "=== All Production Hardening Checks Passed ===" -ForegroundColor Green
