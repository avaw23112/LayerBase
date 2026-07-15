using System.Reflection;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Worker;

namespace EventsTest;

[TestFixture]
public sealed class WorkerEventJobScopeTests
{
    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Worker_job_result_returns_to_origin_scope_inbox_before_event_center()
    {
        var layer = new WorkerProbeLayer();
        var service = new WorkerProbeService();
        var received = new List<WorkerProbeResult>();
        int handlerThreadId = 0;
        layer.RegisterService(service);
        layer.Subscribe((in WorkerProbeResult result) =>
        {
            handlerThreadId = Environment.CurrentManagedThreadId;
            received.Add(result);
        });

        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .Build();

        int ownerThreadId = Environment.CurrentManagedThreadId;
        WorkerHandle handle = service.Run(21);

        Assert.That(SpinUntil(() => runtime.WorkerJobs.GetState(handle) == WorkerState.Completed), Is.True);
        Assert.That(runtime.WorkerJobs.GetState(handle), Is.EqualTo(WorkerState.Completed));
        Assert.That(received, Is.Empty, "Completed only means the result ScopeEvent reached the origin inbox.");

        runtime.Pump(0f);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].Value, Is.EqualTo(42));
        Assert.That(handlerThreadId, Is.EqualTo(ownerThreadId));
        Assert.That(received[0].WorkerThreadId, Is.Not.EqualTo(ownerThreadId));
    }

    [Test]
    public void Worker_job_failure_returns_sanitized_failure_event_to_origin_scope()
    {
        var layer = new WorkerProbeLayer();
        var service = new WorkerProbeService();
        var failures = new List<WorkerJobFailedEvent>();
        layer.RegisterService(service);
        layer.Subscribe((in WorkerJobFailedEvent failure) => failures.Add(failure));

        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .Build();

        WorkerHandle handle = service.RunThrowing();

        Assert.That(SpinUntil(() => runtime.WorkerJobs.GetState(handle) == WorkerState.Failed), Is.True);
        runtime.Pump(0f);

        Assert.That(failures, Has.Count.EqualTo(1));
        Assert.That(failures[0].Handle, Is.EqualTo(handle));
        Assert.That(failures[0].Kind, Is.EqualTo(WorkerJobFailureKind.ExecutionFault));
        Assert.That(failures[0].Error.TypeName, Does.Contain(nameof(InvalidOperationException)));
        Assert.That(failures[0].Error.Message, Is.EqualTo("worker failure"));
    }

    [Test]
    public void Different_scopes_share_scheduler_but_result_returns_only_to_origin_scope()
    {
        using var runtime = new LayerRuntime(24012);
        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(
            runtime,
            CreateTwoScopePlans(240),
            runtime.Id,
            runtime.Generation);

        ScopeRuntime mainScope = host.MainScope;
        ScopeRuntime customScope = host.Scopes[1];
        InitializeScopeEventRuntime(
            mainScope,
            EventTypeId<WorkerProbeResult>.Id);
        InitializeScopeEventRuntime(
            customScope,
            EventTypeId<WorkerProbeResult>.Id);

        int mainValue = 0;
        int customValue = 0;
        mainScope.EventCenter.SubscribeNotify<WorkerProbeResult>(0, (in WorkerProbeResult result) => mainValue = result.Value);
        customScope.EventCenter.SubscribeNotify<WorkerProbeResult>(0, (in WorkerProbeResult result) => customValue = result.Value);

        var service = new WorkerProbeService();
        AttachScopeRuntime(service, runtime, customScope);

        WorkerHandle handle = service.Run(13);

        Assert.That(SpinUntil(() => runtime.WorkerJobs.GetState(handle) == WorkerState.Completed), Is.True);

        mainScope.PumpScopeResources(0f);
        Assert.That(mainValue, Is.EqualTo(0));

        customScope.PumpScopeResources(0f);
        Assert.That(customValue, Is.EqualTo(26));
        Assert.That(mainValue, Is.EqualTo(0));
    }

    [Test]
    public void Worker_result_post_policy_preserves_all_and_latest_on_origin_owner_thread()
    {
        var layer = new WorkerProbeLayer();
        var service = new WorkerProbeService();
        var received = new List<WorkerProbeResult>();
        layer.RegisterService(service);
        layer.Subscribe((in WorkerProbeResult result) => received.Add(result));

        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .Build();

        for (int i = 1; i <= 3; i++)
        {
            WorkerHandle handle = service.Run(i, WorkerEventJobOptions.All);
            Assert.That(SpinUntil(() => runtime.WorkerJobs.GetState(handle) == WorkerState.Completed), Is.True);
            runtime.Pump(0f);
        }

        Assert.That(received.Select(result => result.Value), Is.EquivalentTo(new[] { 2, 4, 6 }));

        received.Clear();
        WorkerHandle latestA = service.Run(4, WorkerEventJobOptions.Latest);
        WorkerHandle latestB = service.Run(5, WorkerEventJobOptions.Latest);
        WorkerHandle latestC = service.Run(6, WorkerEventJobOptions.Latest);

        Assert.That(SpinUntil(() =>
            runtime.WorkerJobs.GetState(latestA) == WorkerState.Completed &&
            runtime.WorkerJobs.GetState(latestB) == WorkerState.Completed &&
            runtime.WorkerJobs.GetState(latestC) == WorkerState.Completed), Is.True);

        runtime.Pump(0f);
        Assert.That(received, Has.Count.EqualTo(1));
    }

    [Test]
    public void Scope_stop_closes_new_worker_job_submission()
    {
        using var runtime = new LayerRuntime(24013);
        using ScopeRuntimeHost host = ScopeRuntimeHost.Create(
            runtime,
            CreateTwoScopePlans(241),
            runtime.Id,
            runtime.Generation);

        ScopeRuntime customScope = host.Scopes[1];
        InitializeScopeEventRuntime(customScope, EventTypeId<WorkerProbeResult>.Id);

        var service = new WorkerProbeService();
        AttachScopeRuntime(service, runtime, customScope);

        _ = customScope.RequestStopAsync();
        customScope.PumpIngress();

        WorkerHandle handle = service.Run(7);

        Assert.That(handle.IsValid, Is.False);
    }

    [Test]
    public void Worker_public_api_keeps_subscribe_parallel_removed_and_scope_ref_closed()
    {
        AssertNoPublicMethodNamed(typeof(Layer), "SubscribeParallel");
        AssertNoPublicMethodNamed(typeof(EventCenter), "SubscribeParallel");

        var assembly = typeof(Layer).Assembly;
        Assert.That(assembly.GetTypes().Any(type => type.Name.Contains("SubscribeParallel")),
            Is.False);
        Assert.That(typeof(ScopeRef<MainScope>)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(method => method.Name == "WorkerJobs"),
            Is.False,
            "Worker job submission should be bound to IService/ILayerContext ownership, not ScopeRef.");
    }

    [Test]
    public void Worker_context_and_scheduler_do_not_expose_scope_resources_to_worker_job()
    {
        string[] forbidden =
        {
            "LayerRuntime",
            "ScopeRuntime",
            "ScopeEndpoint",
            "ScopeRef",
            "EventCenter",
            "PostScheduler",
            "Timer",
            "ServiceProvider",
            "EcsWorld",
            "ActorWorld",
            "LayerToolRegistry"
        };

        var contextMembers = typeof(WorkerJobContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(member => member.Name)
            .ToArray();

        Assert.That(contextMembers, Is.EquivalentTo(new[]
        {
            nameof(WorkerJobContext.WorkerIndex),
            nameof(WorkerJobContext.IsCancellationRequested),
            nameof(WorkerJobContext.CancellationToken)
        }));

        var workerAssembly = typeof(WorkerJobContext).Assembly;
        var workerTypes = workerAssembly.GetTypes()
            .Where(type => type.Namespace == "LayerBase.Worker" && type.Name.Contains("Worker"))
            .ToArray();

        foreach (var type in workerTypes)
        {
            var publicNames = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(member => member.Name)
                .ToArray();

            foreach (string name in forbidden)
            {
                Assert.That(publicNames, Does.Not.Contain(name),
                    $"{type.FullName} should not expose runtime-local resource '{name}'.");
            }
        }
    }

    private static void AssertNoPublicMethodNamed(Type type, string methodName)
    {
        Assert.That(type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Any(method => method.Name == methodName),
            Is.False);
    }

    private static bool SpinUntil(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return true;

            Thread.Sleep(10);
        }

        return predicate();
    }

    private static ScopeExecutionPlan[] CreateTwoScopePlans(int scopeId)
    {
        return new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(scopeId, nameof(WorkerProbeCustomScope), typeof(WorkerProbeCustomScope)),
                ScopeOptions.Inline)
        };
    }

    private static void AttachScopeRuntime(object target, LayerRuntime runtime, ScopeRuntime scope)
    {
        MethodInfo? method = typeof(ServiceLayerBinder).GetMethod(
            "AttachScopeRuntime",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(object), typeof(LayerRuntime), typeof(ScopeRuntime) },
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object[] { target, runtime, scope });
    }

    private static void InitializeScopeEventRuntime(ScopeRuntime scope, params int[] eventTypeIds)
    {
        var options = PostSchedulerOptions.Default;
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        var plans = eventTypeIds
            .Select(eventTypeId => new PostTypePlan(
                eventTypeId,
                PostDeliveryMode.Normal,
                options.DefaultBackpressure,
                maxPending: 0,
                options.DefaultBackpressure))
            .ToArray();

        scope.InitializeOrUpdateScheduler(options, policyTable, plans);
        scope.InitializeTimer(TimeSchedulerOptions.Default);
    }

    private sealed class WorkerProbeLayer : Layer
    {
    }

    private readonly struct WorkerProbeCustomScope : IScopeDefinition
    {
    }

    private sealed class WorkerProbeService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle Run(int value, WorkerEventJobOptions options = default)
        {
            return this.WorkerJobs().Run<WorkerProbeJob, WorkerProbeInput, WorkerProbeResult>(
                new WorkerProbeJob(),
                new WorkerProbeInput(value),
                options);
        }

        public WorkerHandle RunThrowing()
        {
            return this.WorkerJobs().Run<ThrowingWorkerProbeJob, WorkerProbeInput, WorkerProbeResult>(
                new ThrowingWorkerProbeJob(),
                new WorkerProbeInput(0));
        }
    }

    private readonly struct WorkerProbeInput
    {
        public WorkerProbeInput(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct WorkerProbeResult
    {
        public WorkerProbeResult(int value, int workerThreadId)
        {
            Value = value;
            WorkerThreadId = workerThreadId;
        }

        public int Value { get; }

        public int WorkerThreadId { get; }

    }

    private readonly struct WorkerProbeJob : IWorkerEventJob<WorkerProbeInput, WorkerProbeResult>
    {
        public WorkerProbeResult Execute(
            in WorkerProbeInput input,
            in WorkerJobContext context)
        {
            return new WorkerProbeResult(
                input.Value * 2,
                Environment.CurrentManagedThreadId);
        }
    }

    private readonly struct ThrowingWorkerProbeJob : IWorkerEventJob<WorkerProbeInput, WorkerProbeResult>
    {
        public WorkerProbeResult Execute(
            in WorkerProbeInput input,
            in WorkerJobContext context)
        {
            throw new InvalidOperationException("worker failure");
        }
    }
}
