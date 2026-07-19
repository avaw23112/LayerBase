using LayerBase.Scope;
using System.Reflection;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeHostWorkerBoundaryTests
{
    [Test]
    public void Host_does_not_access_scope_transport()
    {
        string source = ReadSource("LayerBase/Scope/ScopeRuntimeHost.cs");

        Assert.That(source, Does.Not.Contain(".Transport."),
            "ScopeRuntimeHost must request stop/dispose through scope control APIs, not close transport directly.");
    }

    [Test]
    public void Host_does_not_call_scope_stop_on_owner_thread()
    {
        string source = ReadSource("LayerBase/Scope/ScopeRuntimeHost.cs");

        Assert.That(source, Does.Not.Contain(".StopOnOwnerThread("),
            "ScopeRuntimeHost must not execute terminal lifecycle steps on behalf of a scope.");
    }

    [Test]
    public void Host_does_not_call_worker_stop()
    {
        string source = ReadSource("LayerBase/Scope/ScopeRuntimeHost.cs");

        Assert.That(source, Does.Not.Contain(".Stop("),
            "Host must request worker exit only after the owning scope has returned a stop response.");
    }

    [Test]
    public void Worker_does_not_expose_stop_api()
    {
        Assert.That(
            typeof(ScopeWorker).GetMethods(BindingFlags.Instance |
                                           BindingFlags.Public |
                                           BindingFlags.NonPublic)
                .Where(method => method.Name == "Stop")
                .Select(method => method.ToString()),
            Is.Empty,
            "ScopeWorker should only expose owner-loop exit coordination, not a host-driven Stop API.");
    }

    [Test]
    public void Worker_finally_does_not_run_scope_lifecycle()
    {
        string source = ReadSource("LayerBase/Scope/ScopeWorker.cs");

        Assert.That(source, Does.Not.Contain("RunRuntimeStop"),
            "RuntimeStop belongs to the scope owner drain protocol, not the worker thread finally block.");
    }

    [Test]
    public void Worker_shutdown_result_type_is_removed()
    {
        Assert.That(typeof(ScopeWorker).Assembly.GetType("LayerBase.Scope.ScopeWorkerShutdownResult"), Is.Null);
    }

    private static string ReadSource(string relativePath)
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "LayerBase.sln")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate repository root.");

        return File.ReadAllText(Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
