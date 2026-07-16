using System.Reflection;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Tools;
using LayerBase.Worker;

namespace EventsTest;

[TestFixture]
public sealed class ScopeFinalAcceptanceTests
{
    [Test]
    public void Scope_runtime_does_not_own_actor_world_or_worker_scheduler()
    {
        var fields = typeof(ScopeRuntime)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(static field => field.FieldType)
            .ToArray();

        Assert.That(fields, Has.No.EqualTo(typeof(ActorWorld)));
        Assert.That(fields, Has.No.EqualTo(typeof(WorkerJobScheduler)));
        Assert.That(fields, Has.No.EqualTo(typeof(Thread)));
    }

    [Test]
    public void Actor_world_does_not_escape_into_scope_or_tool_runtime_owners()
    {
        var forbiddenOwners = ProductionTypes()
            .SelectMany(static type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(static field => field.FieldType == typeof(ActorWorld))
                .Select(field => type.FullName + "." + field.Name))
            .Where(static owner => owner.StartsWith("LayerBase.Scope.", StringComparison.Ordinal) ||
                                   owner.StartsWith("LayerBase.Tools.", StringComparison.Ordinal) ||
                                   owner.StartsWith("LayerBase.ECS.Runtime.", StringComparison.Ordinal) ||
                                   owner.StartsWith("LayerBase.Application.", StringComparison.Ordinal))
            .ToArray();

        Assert.That(forbiddenOwners, Is.Empty);
    }

    [Test]
    public void Cross_scope_has_no_third_business_channel_names()
    {
        string[] forbiddenNames =
        {
            "ScopePostEndpoint",
            "UnifiedCallRoute",
            "DiagnosticsQueue",
            "MetricsThread",
            "GlobalDiagnosticsHub",
            "PostFromAnyThread",
            "SubscribeParallel"
        };

        var typeAndMemberNames = ProductionTypes()
            .SelectMany(static type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(member => type.Name + "." + member.Name)
                .Append(type.Name))
            .ToArray();

        foreach (string forbidden in forbiddenNames)
        {
            Assert.That(typeAndMemberNames.Any(name => name.Contains(forbidden, StringComparison.Ordinal)),
                Is.False,
                forbidden + " must not remain in production runtime architecture.");
        }
    }

    [Test]
    public void Scope_ref_public_surface_only_exposes_address_post_and_call()
    {
        var publicMembers = typeof(ScopeRef<MainScope>)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(static member => member.DeclaringType == typeof(ScopeRef<MainScope>))
            .Select(static member => member.Name)
            .Distinct()
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(publicMembers, Is.EquivalentTo(new[]
        {
            "Address",
            "Call",
            "Post",
            "TryPost",
            "get_Address"
        }));
    }

    [Test]
    public void Local_call_routes_are_scope_owned_not_target_scope_entries()
    {
        var routeEntryTypes = ProductionTypes()
            .Where(static type => type.Name.Contains("LocalCall", StringComparison.Ordinal) &&
                                  type.Name.Contains("Route", StringComparison.Ordinal))
            .ToArray();

        Assert.That(routeEntryTypes, Is.Not.Empty);
        foreach (var type in routeEntryTypes)
        {
            var targetScopeMembers = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(static member => member.Name.Contains("TargetScope", StringComparison.Ordinal))
                .Select(static member => member.Name)
                .ToArray();

            Assert.That(targetScopeMembers, Is.Empty, type.FullName + " must not route LocalCall by TargetScopeId.");
        }
    }

    [Test]
    public void Layer_tool_metadata_keeps_owner_layer_and_cache_state_without_public_runtime_objects()
    {
        var descriptorProperties = typeof(LayerToolDescriptor)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        Assert.That(descriptorProperties, Does.Contain(nameof(LayerToolDescriptor.OwnerLayerIndex)));
        Assert.That(descriptorProperties, Does.Contain(nameof(LayerToolDescriptor.OwnerScopeId)));
        Assert.That(descriptorProperties, Does.Contain(nameof(LayerToolDescriptor.Cache)));
        Assert.That(descriptorProperties, Does.Not.Contain("ScopeRuntime"));
        Assert.That(descriptorProperties, Does.Not.Contain("ServiceProvider"));
        Assert.That(descriptorProperties, Does.Not.Contain("ToolInstance"));
    }

    [Test]
    public void Aot_gate_forbids_runtime_emit_and_dynamic_compilation_in_production_sources()
    {
        string[] forbidden =
        {
            "System.Reflection.Emit",
            "DynamicMethod",
            "Expression.Compile"
        };

        var matches = ProductionSourceFiles()
            .SelectMany(file =>
            {
                string text = File.ReadAllText(file);
                return forbidden
                    .Where(token => text.Contains(token, StringComparison.Ordinal))
                    .Select(token => Path.GetRelativePath(RepositoryRoot(), file) + ": " + token);
            })
            .ToArray();

        Assert.That(matches, Is.Empty);
    }

    [Test]
    public void Running_scope_runtime_sources_do_not_scan_all_assemblies_or_use_make_generic_type()
    {
        string[] runtimeFiles =
        {
            "LayerBase/Application/LayerRuntime.cs",
            "LayerBase/Application/LayerRuntime.Diagnostics.cs",
            "LayerBase/Scope/ScopeRuntime.cs",
            "LayerBase/Scope/ScopeTransport.cs",
            "LayerBase/Event/PostScheduler/PostScheduler.cs",
            "LayerBase/Event/TimeScheduler/TimeScheduler.cs",
            "LayerBase/Event/Delay/DelayPublisherManager.cs"
        };

        string[] forbidden =
        {
            "AppDomain.CurrentDomain.GetAssemblies",
            "Assembly.GetExecutingAssembly",
            "MakeGenericType",
            "Activator.CreateInstance"
        };

        var root = RepositoryRoot();
        var matches = runtimeFiles
            .Select(file => Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)))
            .Where(File.Exists)
            .SelectMany(file =>
            {
                string text = File.ReadAllText(file);
                return forbidden
                    .Where(token => text.Contains(token, StringComparison.Ordinal))
                    .Select(token => Path.GetRelativePath(root, file) + ": " + token);
            })
            .ToArray();

        Assert.That(matches, Is.Empty);
    }

    [Test]
    public void Query_generator_emits_void_entry_points_and_keeps_input_out_of_execute_parameters()
    {
        string generator = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "LayerBase.Generator",
            "LayerBase.Generator",
            "QueryBringGenerator.cs"));

        Assert.That(generator, Does.Contain("public void {entryPoint}"));
        Assert.That(generator, Does.Contain("case QueryUserParameterKind.Input:"));
        Assert.That(generator, Does.Contain("continue;"));
        Assert.That(generator, Does.Contain("private static List<string> BuildExecuteParameters"));
    }

    private static IEnumerable<Type> ProductionTypes()
    {
        return typeof(LayerRuntime).Assembly
            .GetTypes()
            .Where(static type => !type.FullName!.Contains(".Test", StringComparison.Ordinal));
    }

    private static IEnumerable<string> ProductionSourceFiles()
    {
        var root = RepositoryRoot();
        return Directory.EnumerateFiles(Path.Combine(root, "LayerBase"), "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(static file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "LayerBase.sln")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
