param(
    [string[]]$ScanDirectories = @(
        "LayerBase.Usage",
        "LayerBase.TestFixtures.PolicyFeature"
    )
)

$ErrorActionPreference = "Stop"
$exitCode = 0

$forbiddenPatterns = @(
    @{ Pattern = ': IAssemblyModule'; Description = 'User-authored IAssemblyModule implementation' },
    @{ Pattern = 'new AssemblyModuleManifest('; Description = 'User-authored AssemblyModuleManifest construction' },
    @{ Pattern = 'EventContribution.ForTypes('; Description = 'User-authored EventContribution construction' },
    @{ Pattern = 'AssemblyModuleManifest Manifest'; Description = 'User-authored Manifest property' },
    @{ Pattern = 'static .*Module Instance'; Description = 'User-authored static Module Instance' }
)

$requiredPattern = @{ Pattern = '\[AssemblyModule\('; Description = 'AssemblyModule attribute' }

$hasRequired = $false

foreach ($dir in $ScanDirectories)
{
    $fullPath = Join-Path -Path $PSScriptRoot -ChildPath "..\$dir"
    if (-not (Test-Path -LiteralPath $fullPath))
    {
        Write-Warning "Directory not found: $fullPath"
        continue
    }

    $files = Get-ChildItem -Path $fullPath -Recurse -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\.g\.cs$' }

    foreach ($file in $files)
    {
        $content = Get-Content -LiteralPath $file.FullName -Raw

        if ($content -match $requiredPattern.Pattern)
        {
            $hasRequired = $true
        }

        foreach ($pattern in $forbiddenPatterns)
        {
            if ($content -match $pattern.Pattern)
            {
                Write-Error "VERIFICATION FAILED: $($file.FullName) contains forbidden pattern '$($pattern.Description)'"
                Write-Error "  Matched: '$($Matches[0])'"
                $exitCode = 1
            }
        }
    }
}

if (-not $hasRequired)
{
    Write-Error "VERIFICATION FAILED: No [AssemblyModule(...)] attribute found in scan directories"
    $exitCode = 1
}

if ($exitCode -eq 0)
{
    Write-Host "PASS: All generated assembly module checks passed." -ForegroundColor Green
}

exit $exitCode
