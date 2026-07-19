$ErrorActionPreference = "Stop"

$packages = Get-ChildItem artifacts/packages -Filter *.nupkg

if ($packages.Count -eq 0) {
    throw "No NuGet packages found."
}

foreach ($pkg in $packages) {
    Write-Host "Package: $($pkg.Name) - $($pkg.Length) bytes"
}

dotnet list LayerBase/LayerBase.csproj package --vulnerable --include-transitive

Write-Host "Package verification passed."
