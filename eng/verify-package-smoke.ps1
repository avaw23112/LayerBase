param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repoRoot "artifacts/package-smoke"
$packageDir = Join-Path $artifactRoot "packages"
$consumerDir = Join-Path $artifactRoot "consumer"

if (Test-Path $artifactRoot) {
    Remove-Item $artifactRoot -Recurse -Force
}

New-Item $packageDir -ItemType Directory -Force | Out-Null
New-Item $consumerDir -ItemType Directory -Force | Out-Null

[xml]$layerBaseProject = Get-Content (
    Join-Path $repoRoot "LayerBase/LayerBase.csproj"
)

$version = [string](
    $layerBaseProject.Project.PropertyGroup |
    Where-Object { $_.Version } |
    Select-Object -First 1
).Version

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "LayerBase package version could not be resolved."
}

$projects = @(
    "LayerBase.Task/LayerBase.Task.csproj",
    "LayerBase.Generator/LayerBase.Generator/LayerBase.Generator.csproj",
    "LayerBase/LayerBase.csproj"
)

foreach ($project in $projects) {
    dotnet pack (Join-Path $repoRoot $project) `
        --configuration $Configuration `
        --output $packageDir `
        -p:GeneratePackageOnBuild=false

    if ($LASTEXITCODE -ne 0) {
        throw "Packing failed: $project"
    }
}

dotnet new console `
    --name LayerBase.PackageSmoke `
    --framework net8.0 `
    --output $consumerDir `
    --force

$consumerProject = Join-Path $consumerDir "LayerBase.PackageSmoke.csproj"

dotnet add $consumerProject package LayerBase `
    --version $version `
    --source $packageDir `
    --no-restore

dotnet add $consumerProject package LayerBase.Generator `
    --version $version `
    --source $packageDir `
    --no-restore

$program = @'
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace LayerBase.PackageSmoke;

public partial struct PackageSmokeEvent
{
    public int Value;
}

public sealed class PackageSmokeEventMetaData
    : EventMetaData<PackageSmokeEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 0);
}

public sealed class PackageSmokeLayer : Layer
{
}

public sealed partial class PackageSmokeService : IService
{
    public int LastValue { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [Subscribe]
    private void OnEvent(in PackageSmokeEvent value)
    {
        LastValue = value.Value;
    }
}

internal static class Program
{
    private static int Main()
    {
        LayerHub.Reset();

        var service = new PackageSmokeService();
        var layer = new PackageSmokeLayer();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        runtime.Post(new PackageSmokeEvent { Value = 1 });
        runtime.Post(new PackageSmokeEvent { Value = 2 });
        runtime.Pump(0.016f);

        if (service.LastValue != 2)
        {
            Console.Error.WriteLine(
                $"Latest policy failed. Expected 2, actual {service.LastValue}.");
            return 1;
        }

        string policyMarkdown = runtime.GetPolicyMarkdown();

        if (!policyMarkdown.Contains(
                nameof(PackageSmokeEvent),
                StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                "Generated EventMetaData was not included in policy output.");
            return 2;
        }

        if (!policyMarkdown.Contains(
                nameof(PostDeliveryMode.Latest),
                StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                "Latest EventMetaData policy was not active.");
            return 3;
        }

        Console.WriteLine("LayerBase package smoke test passed.");
        return 0;
    }
}
'@

Set-Content `
    -Path (Join-Path $consumerDir "Program.cs") `
    -Value $program `
    -Encoding UTF8

dotnet restore $consumerProject `
    --source $packageDir `
    --source "https://api.nuget.org/v3/index.json"

if ($LASTEXITCODE -ne 0) {
    throw "Package consumer restore failed."
}

dotnet run `
    --project $consumerProject `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "Package consumer execution failed."
}
