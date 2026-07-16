using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeArchitectureAcceptanceTests
{
    [Test]
    public void Public_local_call_api_does_not_expose_target_layer_type_parameter()
    {
        AssertNoTargetLayerCallAsync(typeof(LayerHub));
        AssertNoTargetLayerCallAsync(typeof(LayerRuntime));

        var targetLayerCallExtension = typeof(ScopeLocalCallHandlerExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "Call" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 3);

        Assert.That(targetLayerCallExtension, Is.Null);
    }

    [Test]
    public void Public_post_api_does_not_expose_any_thread_ingress()
    {
        AssertNoPublicMethodNamed(typeof(LayerHub), "PostFromAnyThread");
        AssertNoPublicMethodNamed(typeof(LayerHub), "TryPostFromAnyThread");
        AssertNoPublicMethodNamed(typeof(LayerRuntime), "PostFromAnyThread");
        AssertNoPublicMethodNamed(typeof(LayerRuntime), "TryPostFromAnyThread");
    }

    [Test]
    public void Public_event_api_does_not_expose_subscribe_parallel()
    {
        AssertNoPublicMethodNamed(typeof(Layer), "SubscribeParallel");
        AssertNoPublicMethodNamed(typeof(EventCenter), "SubscribeParallel");
    }

    [Test]
    public void Event_center_does_not_keep_internal_parallel_subscription_pipeline()
    {
        AssertNoMethodNamed(typeof(EventCenter), "SubscribeParallel");
        AssertNoMethodNamed(typeof(EventCenter), "UnsubscribeParallel");

        var eventAssembly = typeof(EventCenter).Assembly;
        Assert.That(eventAssembly.GetTypes().Any(t => t.Name.Contains("ParallelHandlerEntry")),
            Is.False,
            "ParallelHandlerEntry should be removed.");
        Assert.That(eventAssembly.GetTypes().Any(t => t.Name.Contains("ParallelSubscriptionQueue")),
            Is.False,
            "ParallelSubscriptionQueue should be removed.");
    }

    [Test]
    public void Post_scheduler_does_not_keep_cross_thread_ingress_pipeline()
    {
        AssertNoFieldNamed(typeof(PostSchedulerOptions), "MaxIngressPostsPerPump");

        var eventAssembly = typeof(EventCenter).Assembly;
        Assert.That(eventAssembly.GetTypes().Any(t => t.Name.Contains("PostIngressQueue")),
            Is.False,
            "PostIngressQueue should be removed.");
        Assert.That(eventAssembly.GetTypes().Any(t => t.Name.Contains("IngressPostItem")),
            Is.False,
            "IngressPostItem should be removed.");
    }

    [Test]
    public void Runtime_always_exposes_main_scope_ref_and_builds_internal_scope_runtime()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeArchitectureLayer())
                              .Build();

        Assert.That(runtime.Main.Address.RuntimeId, Is.EqualTo(runtime.Id));
        Assert.That(runtime.Main.Address.ScopeId, Is.EqualTo(MainScope.ScopeId));
        Assert.That(runtime.TryGetScope<MainScope>(out var main), Is.True);
        Assert.That(main.Address, Is.EqualTo(runtime.Main.Address));

        AssertNoPublicMethodNamed(typeof(ScopeRef<MainScope>), "GetService");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeRuntime"),
            Is.Not.Null,
            "ScopeRuntime should exist as the internal runtime resource owner.");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeRuntimeHost"),
            Is.Not.Null,
            "ScopeRuntimeHost should own ScopeRuntime creation and lookup.");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeTransport"),
            Is.Not.Null,
            "ScopeTransport should own Scope event/call inboxes and the endpoint.");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeExecutionPlan"),
            Is.Not.Null,
            "ScopeRuntime should be created from a ScopeExecutionPlan.");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeOptions"),
            Is.Not.Null,
            "ScopeOptions should describe Main/Inline/Worker execution resources.");
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeWorker"),
            Is.Not.Null,
            "Worker threads should be owned by ScopeWorker, not ScopeRuntime.");
    }

    [Test]
    public void Layer_runtime_public_api_does_not_expose_scope_local_resources_or_root_provider()
    {
        string[] forbidden =
        {
            "EventCenter",
            "Scheduler",
            "Timer",
            "EcsWorld",
            "Actors",
            "ServiceProvider",
            "GetService",
            "Send",
            "Post",
            "TryPost",
            "MarkDirty",
            "PostLatest",
            "PostCoalesced",
            "SchedulePost",
            "CreateActor",
            "AskActor",
            "PostTo",
            "PostToMany",
            "PolicyTable",
            "RebuildEventPolicies",
            "ReportInfo",
            "CallAsync"
        };

        var publicMembers = typeof(LayerRuntime)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(static member => member.Name)
            .ToArray();

        foreach (string name in forbidden)
        {
            Assert.That(publicMembers, Does.Not.Contain(name),
                $"LayerRuntime must expose ScopeRef/control API instead of raw runtime resource '{name}'.");
        }
    }

    [Test]
    public void Layer_hub_public_api_does_not_expose_primary_runtime_event_or_call_shortcuts()
    {
        string[] forbidden =
        {
            "Send",
            "Post",
            "TryPost",
            "MarkDirty",
            "PostLatest",
            "PostCoalesced",
            "CallAsync"
        };

        var publicMembers = typeof(LayerHub)
            .GetMembers(BindingFlags.Public | BindingFlags.Static)
            .Select(static member => member.Name)
            .ToArray();

        foreach (string name in forbidden)
        {
            Assert.That(publicMembers, Does.Not.Contain(name),
                $"LayerHub must expose runtime creation/control, not primary-runtime shortcut '{name}'.");
        }
    }

    [Test]
    public void Main_scope_ingress_and_runtime_resources_are_owned_by_scope_runtime_not_layer_runtime_fields()
    {
        var runtimeInboxFields = typeof(LayerRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field =>
                field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(ScopeBoundedInbox<>))
            .Select(field => field.Name)
            .ToArray();

        Assert.That(runtimeInboxFields, Is.Empty,
            "LayerRuntime should not directly own Scope inboxes; they belong to ScopeTransport.");

        var runtimeResourceFieldTypes = typeof(LayerRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.That(runtimeResourceFieldTypes, Has.No.EqualTo(typeof(EventCenter)));
        Assert.That(runtimeResourceFieldTypes, Has.No.EqualTo(typeof(PostScheduler)));
        Assert.That(runtimeResourceFieldTypes, Has.None.Matches<Type>(type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TimeScheduler<>)));
        Assert.That(runtimeResourceFieldTypes, Has.No.EqualTo(typeof(ScopeLocalCallRegistry)));

        var scopeRuntimeType = typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.ScopeRuntime")!;
        var scopeRuntimeFieldTypes = scopeRuntimeType
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.That(scopeRuntimeFieldTypes, Has.No.EqualTo(typeof(Thread)));

        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.Scope.MainScopeExecution"),
            Is.Null,
            "MainScopeExecution was an interim boundary and must not remain after ScopeRuntime is introduced.");
    }

    [Test]
    public void Scope_endpoint_writers_do_not_hold_scope_runtime_references()
    {
        AssertNoRuntimeReferenceFields(typeof(RuntimeScopeEventWriter));
        AssertNoRuntimeReferenceFields(typeof(RuntimeScopeCallWriter));

        AssertNoMethodNamed(typeof(RuntimeScopeEventWriter), "Attach");
        AssertNoMethodNamed(typeof(RuntimeScopeEventWriter), "Detach");
        AssertNoMethodNamed(typeof(RuntimeScopeCallWriter), "Attach");
        AssertNoMethodNamed(typeof(RuntimeScopeCallWriter), "Detach");
        AssertNoMethodNamed(typeof(ScopeTransport), "AttachRuntime");
    }

    [Test]
    public void Scope_transport_owns_protocol_payload_storage()
    {
        var transportStorageFields = typeof(ScopeTransport)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(EventPayloadStorage))
            .Select(field => field.Name)
            .ToArray();

        Assert.That(transportStorageFields, Is.EquivalentTo(new[]
        {
            "_eventPayloadStorage",
            "_callPayloadStorage"
        }));

        var runtimeStorageFields = typeof(LayerRuntime).Assembly
            .GetType("LayerBase.Scope.ScopeRuntime")!
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(EventPayloadStorage))
            .Select(field => field.Name)
            .ToArray();

        Assert.That(runtimeStorageFields, Is.Empty,
            "ScopeRuntime should consume protocol storage through ScopeTransport instead of owning it.");
    }

    [Test]
    public void Scope_ref_only_carries_endpoint_not_runtime_resources()
    {
        var fieldTypes = typeof(ScopeRef<MainScope>)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.That(fieldTypes, Is.EquivalentTo(new[] { typeof(ScopeEndpoint) }));
        Assert.That(fieldTypes, Has.No.EqualTo(typeof(ScopeRuntime)));
        Assert.That(fieldTypes, Has.No.EqualTo(typeof(EventCenter)));
        Assert.That(fieldTypes, Has.No.EqualTo(typeof(PostScheduler)));
        Assert.That(fieldTypes, Has.No.EqualTo(typeof(ScopeLocalCallRegistry)));
    }

    [Test]
    public void Main_scope_ref_try_post_uses_scope_event_inbox_path()
    {
        LayerHub.Reset();

        var layer = new ScopeArchitectureLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        Assert.That(runtime.Main.TryPost(new ScopeArchitectureEvent(42)), Is.True);

        runtime.Pump(0.016f);

        Assert.That(layer.LastValue, Is.EqualTo(42));
    }

    [Test]
    public void Main_scope_ref_post_reports_accepted_result()
    {
        LayerHub.Reset();

        var layer = new ScopeArchitectureLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        var result = runtime.Main.Post(new ScopeArchitectureEvent(43));

        Assert.That(result.IsAccepted, Is.True);
        Assert.That(result.Status, Is.EqualTo(ScopePostStatus.Accepted));

        runtime.Pump(0.016f);

        Assert.That(layer.LastValue, Is.EqualTo(43));
    }

    [Test]
    public void Main_scope_ref_post_enters_scope_event_inbox_before_local_post_scheduler()
    {
        LayerHub.Reset();

        var layer = new ScopeArchitectureLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        var result = runtime.Main.Post(new ScopeArchitectureEvent(44));
        runtime.Scheduler.Pump();

        Assert.That(result.Status, Is.EqualTo(ScopePostStatus.Accepted));
        Assert.That(layer.LastValue, Is.EqualTo(0));
    }

    [Test]
    public void Main_scope_ref_try_post_rejects_after_runtime_dispose()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeArchitectureLayer())
                              .Build();
        var main = runtime.Main;

        runtime.Dispose();

        Assert.That(main.TryPost(new ScopeArchitectureEvent(7)), Is.False);
    }

    [Test]
    public void Main_scope_ref_post_reports_runtime_disposed_after_dispose()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeArchitectureLayer())
                              .Build();
        var main = runtime.Main;

        runtime.Dispose();

        var result = main.Post(new ScopeArchitectureEvent(7));

        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Status, Is.EqualTo(ScopePostStatus.RuntimeDisposed));
    }

    [Test]
    public void Main_scope_ref_does_not_keep_disposed_runtime_alive()
    {
        LayerHub.Reset();

        var retainedScope = CreateDisposedRuntimeScope(out var weakRuntime);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(weakRuntime.TryGetTarget(out _), Is.False);
        Assert.That(retainedScope.TryPost(new ScopeArchitectureEvent(7)), Is.False);
    }

    [Test]
    public void Layer_scope_ref_targets_owner_runtime_main_scope()
    {
        LayerHub.Reset();

        var layer = new ScopeArchitectureLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        var scope = layer.Scope<MainScope>();

        Assert.That(scope.Address, Is.EqualTo(runtime.Main.Address));
        Assert.That(scope.TryPost(new ScopeArchitectureEvent(64)), Is.True);

        runtime.Pump(0.016f);

        Assert.That(layer.LastValue, Is.EqualTo(64));
    }

    [Test]
    public void Layer_scope_ref_requires_attached_runtime()
    {
        var layer = new ScopeArchitectureLayer();

        Assert.Throws<InvalidOperationException>(() => layer.Scope<MainScope>());
    }

    [Test]
    public async Task Main_scope_ref_call_uses_subscribe_scope_call_registry()
    {
        LayerHub.Reset();

        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        var runtime = LayerHub.CreateLayers()
                              .Push(coreLayer)
                              .Build();

        var task = runtime.Main.Call<SwitchSceneRequest, SwitchSceneResponse>(
            new SwitchSceneRequest("ScopeRefCallScene"));
        runtime.Pump(0.016f);

        var response = await task;

        Assert.That(response.SceneName, Is.EqualTo("ScopeRefCallScene"));
        Assert.That(response.Success, Is.True);
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("ScopeRefCallScene"));
    }

    [Test]
    public async Task Main_scope_ref_call_enters_scope_call_inbox_before_local_call_registry()
    {
        LayerHub.Reset();

        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        var runtime = LayerHub.CreateLayers()
                              .Push(coreLayer)
                              .Build();

        var task = runtime.Main.Call<ScopeDeferredCallRequest, ScopeDeferredCallResponse>(
            new ScopeDeferredCallRequest("DeferredScopeCallScene"));

        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo(string.Empty));
        Assert.That(task.GetAwaiter().IsCompleted, Is.False);

        runtime.Pump(0.016f);

        var response = await task;

        Assert.That(response.SceneName, Is.EqualTo("DeferredScopeCallScene"));
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("DeferredScopeCallScene"));
    }

    [Test]
    public async Task Layer_scope_ref_call_uses_owner_runtime_main_scope()
    {
        LayerHub.Reset();

        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers()
                .Push(coreLayer)
                .Build();

        var task = coreLayer.Scope<MainScope>().Call<SwitchSceneRequest, SwitchSceneResponse>(
            new SwitchSceneRequest("LayerScopeRefCallScene"));
        LayerHub.Pump(0.016f);

        var response = await task;

        Assert.That(response.SceneName, Is.EqualTo("LayerScopeRefCallScene"));
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("LayerScopeRefCallScene"));
    }

    private static void AssertNoTargetLayerCallAsync(Type type)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault(method =>
                method.Name == "CallAsync" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 3);

        Assert.That(method, Is.Null, $"{type.Name} still exposes a TLayer CallAsync overload.");
    }

    private static void AssertNoPublicMethodNamed(Type type, string methodName)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault(method => method.Name == methodName);

        Assert.That(method, Is.Null, $"{type.Name} still exposes {methodName}.");
    }

    private static void AssertNoMethodNamed(Type type, string methodName)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault(method => method.Name == methodName);

        Assert.That(method, Is.Null, $"{type.Name} still contains {methodName}.");
    }

    private static void AssertNoFieldNamed(Type type, string fieldName)
    {
        var field = type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault(field => field.Name == fieldName);

        Assert.That(field, Is.Null, $"{type.Name} still contains {fieldName}.");
    }

    private static void AssertNoRuntimeReferenceFields(Type type)
    {
        var forbiddenFields = type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(field => ReferencesScopeRuntime(field.FieldType))
            .Select(field => field.Name)
            .ToArray();

        Assert.That(forbiddenFields, Is.Empty,
            $"{type.Name} must write to the transport boundary, not retain ScopeRuntime fields.");
    }

    private static bool ReferencesScopeRuntime(Type type)
    {
        if (type == typeof(ScopeRuntime))
            return true;

        if (!type.IsGenericType)
            return false;

        return type.GetGenericArguments().Any(static argument => argument == typeof(ScopeRuntime));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static ScopeRef<MainScope> CreateDisposedRuntimeScope(out WeakReference<LayerRuntime> weakRuntime)
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeArchitectureLayer())
                              .Build();
        var scope = runtime.Main;
        weakRuntime = new WeakReference<LayerRuntime>(runtime);

        runtime.Dispose();
        runtime = null!;

        return scope;
    }

    private sealed class ScopeArchitectureLayer : Layer, IAutoScopeEndpointBinder
    {
        public int LastValue { get; private set; }

        public ScopeArchitectureLayer()
        {
            Subscribe<ScopeArchitectureEvent>(OnEvent);
        }

        void IAutoScopeEndpointBinder.AutoBindScopeEndpoints(Layer layer)
        {
            ScopeEventRegistrationBridge.RegisterForOwner(
                layer,
                this,
                new ScopeArchitectureEventHandler(this));
        }

        private void OnEvent(in ScopeArchitectureEvent value)
        {
            LastValue = value.Value;
        }

        private sealed class ScopeArchitectureEventHandler : IScopeEventHandler<ScopeArchitectureEvent>
        {
            private readonly ScopeArchitectureLayer _owner;

            public ScopeArchitectureEventHandler(ScopeArchitectureLayer owner)
            {
                _owner = owner;
            }

            public void Handle(in ScopeArchitectureEvent value)
            {
                _owner.OnEvent(in value);
            }
        }
    }

    private readonly struct ScopeArchitectureEvent
    {
        public readonly int Value;

        public ScopeArchitectureEvent(int value)
        {
            Value = value;
        }
    }

}

public readonly struct ScopeDeferredCallRequest
{
    public readonly string SceneName;

    public ScopeDeferredCallRequest(string sceneName)
    {
        SceneName = sceneName;
    }
}

public readonly struct ScopeDeferredCallResponse
{
    public readonly string SceneName;

    public ScopeDeferredCallResponse(string sceneName)
    {
        SceneName = sceneName;
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class ScopeDeferredCallHandler
    : IScopeLocalCallHandler<ScopeDeferredCallRequest, ScopeDeferredCallResponse>
{
    public async LBTask<ScopeDeferredCallResponse> HandleAsync(
        ScopeDeferredCallRequest request,
        CancellationToken cancellationToken = default)
    {
        await LBTask.CompletedTask;
        var sceneService = this.Get<SceneService>();
        sceneService.SwitchTo(request.SceneName);
        return new ScopeDeferredCallResponse(sceneService.LastScene);
    }
}
