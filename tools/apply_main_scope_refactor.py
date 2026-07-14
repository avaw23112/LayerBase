from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected exactly one match, found {count}")
    return text.replace(old, new, 1)


def regex_once(text: str, pattern: str, replacement: str, label: str) -> str:
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
    if count != 1:
        raise RuntimeError(f"{label}: expected exactly one regex match, found {count}")
    return updated


def patch_layer_runtime() -> None:
    path = "LayerBase/Application/LayerRuntime.cs"
    text = read(path)

    text = replace_once(
        text,
        "/// LayerBase 的运行时实例。每个 LayerRuntime 拥有独立的 Layer 链、事件中心、\n"
        "/// 调度器、定时器、Actor 世界、ECS 世界和服务容器。\n",
        "/// LayerBase 的应用级运行时聚合根。LayerRuntime 管理 Layer 层级、ScopeHost、\n"
        "/// ActorWorld、Worker、异常汇总与全局工具；业务资源统一归属各 ScopeRuntime。\n",
        "LayerRuntime summary",
    )

    text = replace_once(text, "    internal EventCenter EventCenter { get; set; }\n", "", "remove runtime EventCenter storage")
    text = replace_once(text, "    private readonly PostIngressQueue _postIngress = new();\n", "", "remove runtime ingress storage")
    text = replace_once(text, "    private Action<Exception>? _completionExceptionHandler;\n", "", "remove runtime completion handler")

    text = replace_once(
        text,
        "    private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Disabled;\n",
        "    private FixedUpdateOptions _fixedUpdateOptions = FixedUpdateOptions.Disabled;\n"
        "    private PostSchedulerOptions _postOptions = PostSchedulerOptions.Default;\n"
        "    private TimeSchedulerOptions _timerOptions = TimeSchedulerOptions.Default;\n"
        "    private DelayBufferOptions _delayOptions = DelayBufferOptions.Default;\n"
        "    private EcsRuntimeOptions _ecsOptions = EcsRuntimeOptions.Default;\n",
        "add scope resource configuration",
    )

    text = replace_once(
        text,
        "    internal LayerBaseSynchronizationContext? _context;\n"
        "    private PostScheduler? _scheduler;\n"
        "    private TimeScheduler<ITimerAction>? _timer;\n"
        "    private RuntimeTimerSink? _timerSink;\n",
        "",
        "remove runtime business subsystem fields",
    )
    text = replace_once(text, "    internal DelayPublisherManager? DelayManager { get; private set; }\n", "", "remove runtime delay storage")

    text = replace_once(
        text,
        "    internal PostScheduler Scheduler => _scheduler ?? throw new InvalidOperationException(\"Runtime not built.\");\n\n"
        "    internal TimeScheduler<ITimerAction> Timer => _timer ?? throw new InvalidOperationException(\"Runtime not built.\");\n",
        "    /// <summary>Runtime 的默认业务执行域。成功 Build 后必然存在且 ScopeId 固定为 0。</summary>\n"
        "    internal ScopeRuntime MainScope\n"
        "    {\n"
        "        get\n"
        "        {\n"
        "            ScopeRuntimeHost host = ScopeHost\n"
        "                ?? throw new InvalidOperationException(\"Runtime not built.\");\n"
        "            if (!host.TryGetScope(ScopeDescriptors.Main.ScopeId, out ScopeRuntime scope))\n"
        "            {\n"
        "                throw new InvalidOperationException(\"MainScope is missing from the runtime host.\");\n"
        "            }\n\n"
        "            return scope;\n"
        "        }\n"
        "    }\n\n"
        "    // 兼容入口只做转发，不再拥有独立资源。\n"
        "    internal EventCenter EventCenter => MainScope.EventCenter;\n"
        "    internal PostScheduler Scheduler => MainScope.PostScheduler;\n"
        "    internal TimeScheduler<ITimerAction> Timer => MainScope.Timer;\n"
        "    internal DelayPublisherManager DelayManager => MainScope.DelayManager;\n",
        "replace runtime resource properties with MainScope routing",
    )

    text = replace_once(text, "        EventCenter = new EventCenter();\n", "", "remove EventCenter construction")
    text = replace_once(text, "        InitializeEcsWorld();\n", "", "remove ECS construction")
    text = replace_once(
        text,
        "        _completionExceptionHandler = ex => ReportLayerEventError(-1, \"System\", \"Completion\", ex);\n",
        "",
        "remove completion callback construction",
    )

    policy_block = r"    private EventBuildPolicyTable\? _policyTable;\n.*?    internal void BuildServiceProvider\(\)"
    policy_replacement = """    public EventBuildPolicyTable PolicyTable => MainScope.PolicyTable;

    internal void InitializeScheduler(PostSchedulerOptions options)
    {
        _postOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void RebuildEventPolicies()
    {
        MainScope.RebuildEventPolicies();
    }

    internal void InitializeTimer(TimeSchedulerOptions options)
    {
        _timerOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    internal void InitializeDelay(DelayBufferOptions options)
    {
        _delayOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    private ScopeRuntimeOptions CreateScopeRuntimeOptions()
    {
        return new ScopeRuntimeOptions(
            postSchedulerOptions: _postOptions,
            timeSchedulerOptions: _timerOptions,
            delayBufferOptions: _delayOptions,
            ecsOptions: _ecsOptions);
    }

    internal void BuildServiceProvider()"""
    text = regex_once(text, policy_block, policy_replacement, "replace runtime resource initialization")

    initialize_scope_host = r"    private void InitializeScopeHost\(\)\n    \{.*?\n    \}\n\n    private bool TryBuildFromInstalledModules\(\)"
    initialize_scope_host_replacement = """    private void InitializeScopeHost()
    {
        if (_chain == null)
        {
            throw new InvalidOperationException("Layer chain is not initialized.");
        }

        // Module 路径同样必须创建 MainScope；自定义 Scope 只是追加执行域。
        if (_installedModules != null && _installedModules.Count > 0)
        {
            if (TryBuildFromInstalledModules())
            {
                return;
            }
        }

        ScopeHostFactoryDelegate? generatedScopeHostFactory = CreateGeneratedScopeHostFactory();
        var services = new List<LayerBase.DI.IService>();
        var seen = new HashSet<object>(LayerBase.Snap.ReferenceEqualityComparer.Instance);

        foreach (Layer layer in _chain.GetNodes())
        {
            foreach (LayerBase.DI.IService service in layer.GetResolvedServices())
            {
                // 未声明 [Scope<T>] 的 Service 默认进入 MainScope。
                if (seen.Add(service))
                {
                    services.Add(service);
                }
            }
        }

        ScopeRuntimeOptions options = CreateScopeRuntimeOptions();
        ScopeHost = generatedScopeHostFactory?.Invoke(services, options, Actors, this)
                    ?? ScopeRuntimeHost.Create(
                        ScopeRuntimePlanner.Build(services),
                        options,
                        sharedActorWorld: Actors,
                        owningRuntime: this);
    }

    private bool TryBuildFromInstalledModules()"""
    text = regex_once(text, initialize_scope_host, initialize_scope_host_replacement, "make MainScope mandatory")

    text = replace_once(
        text,
        "        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(_installedModules);\n"
        "        if (catalog.ScopeDefinitions.Count == 0)\n"
        "        {\n"
        "            return false;\n"
        "        }\n\n",
        "        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(_installedModules);\n",
        "remove module scope-definition gate",
    )
    text = replace_once(
        text,
        "            CreateModuleCallDispatchers(catalog),\n"
        "            CreateModuleEventDispatchers(catalog));\n",
        "            CreateModuleCallDispatchers(catalog),\n"
        "            CreateModuleEventDispatchers(catalog),\n"
        "            options: CreateScopeRuntimeOptions());\n",
        "pass Scope options to module host",
    )

    text = replace_once(
        text,
        "        _scheduler?.BuildPlans(Array.Empty<PostTypePlan>());\n"
        "        if (ScopeHost == null)\n",
        "        if (ScopeHost == null)\n",
        "remove runtime scheduler prewarm",
    )

    pump_region = r"    public void Pump\(float deltaTime\)\n    \{.*?\n    \}\n    #endregion\n\n    #region Public API - Event Send / Post"
    pump_replacement = """    public void Pump(float deltaTime)
    {
        if (_disposed) return;

        using var runtimeScope = LayerRuntimeExecution.Enter(this);
        PumpCore(deltaTime);
    }

    private void PumpCore(float deltaTime)
    {
        ScopeRuntime mainScope = MainScope;

        // Worker 产生的普通业务事件只进入 MainScope 的 PostScheduler。
        Worker.DrainEventsTo(
            mainScope.PostScheduler,
            mainScope.PostScheduler.Options.MaxIngressPostsPerPump);

        // 所有事件、Timer、Delay、ECS、Continuation 与业务生命周期都由 Scope 推进。
        ScopeHost!.Pump(deltaTime);

        RuntimeFrameBudget actorBudget = default;
        DrainActorCommands();
        Actors.Pump(
            deltaTime: deltaTime,
            fixedDeltaTime: _fixedUpdateOptions.Enabled ? _fixedUpdateOptions.FixedDeltaTime : 0f,
            pumpFixedUpdate: _fixedUpdateOptions.Enabled,
            budget: ref actorBudget);

        TryDrainExceptions();
    }

    /// <summary>
    /// 由 MainScope 在自己的 ScopeExecution 与同步上下文内调用。
    /// LayerRuntime 只保留 Layer 层级推进，不再推进任何业务资源。
    /// </summary>
    internal void PumpMainScopeLayers(float deltaTime)
    {
        if (_fixedUpdateOptions.Enabled)
        {
            _fixedUpdateAccumulator += deltaTime;
            int steps = 0;
            while (_fixedUpdateAccumulator >= _fixedUpdateOptions.FixedDeltaTime &&
                   steps < _fixedUpdateOptions.MaxStepsPerPump)
            {
                MainScope.PumpFixedServices(_fixedUpdateOptions.FixedDeltaTime);
                _chain?.PumpFixed(_fixedUpdateOptions.FixedDeltaTime);
                _fixedUpdateAccumulator -= _fixedUpdateOptions.FixedDeltaTime;
                steps++;
            }
        }

        _chain?.Pump(deltaTime);
    }
    #endregion

    #region Public API - Event Send / Post"""
    text = regex_once(text, pump_region, pump_replacement, "replace runtime pump with ScopeHost pump")

    text = replace_once(
        text,
        "        if (_disposed) return;\n"
        "        _postIngress.Enqueue(value, policy);\n",
        "        if (_disposed) return;\n"
        "        MainScope.PostFromAnyThread(value, policy);\n",
        "route PostFromAnyThread to MainScope",
    )
    text = replace_once(
        text,
        "        if (_disposed) return false;\n"
        "        return _postIngress.Enqueue(value, policy);\n",
        "        if (_disposed) return false;\n"
        "        return MainScope.TryPostFromAnyThread(value, policy);\n",
        "route TryPostFromAnyThread to MainScope",
    )
    text = text.replace("_policyTable?.GetTimerPolicy(eventId)", "PolicyTable.GetTimerPolicy(eventId)")

    for old in [
        "                Capture(_postIngress.Clear);\n",
        "                Capture(EcsScheduler.Dispose);\n",
        "                Capture(EcsWorld.Dispose);\n",
        "                Capture(() => _scheduler?.Dispose());\n",
        "                Capture(() => _timer?.Dispose());\n",
        "                Capture(() => DelayManager?.Clear());\n",
        "                DelayManager = null;\n",
        "                Capture(EventCenter.Reset);\n",
        "                Capture(() => _context?.Dispose());\n",
        "            Capture(_postIngress.Clear);\n",
        "            Capture(EcsScheduler.Dispose);\n",
        "            Capture(EcsWorld.Dispose);\n",
        "            Capture(() => _scheduler?.Dispose());\n",
        "            Capture(() => _timer?.Dispose());\n",
        "            Capture(() => DelayManager?.Clear());\n",
        "            DelayManager = null;\n",
        "            Capture(EventCenter.Reset);\n",
        "            Capture(() => _context?.Dispose());\n",
    ]:
        text = text.replace(old, "")

    text = replace_once(
        text,
        "        if (_policyTable == null)\n"
        "        {\n"
        "            return \"Runtime not built.\";\n"
        "        }\n\n"
        "        var sb = new StringBuilder();\n",
        "        if (ScopeHost == null)\n"
        "        {\n"
        "            return \"Runtime not built.\";\n"
        "        }\n\n"
        "        EventBuildPolicyTable policyTable = PolicyTable;\n"
        "        var sb = new StringBuilder();\n",
        "route policy diagnostics to MainScope",
    )
    text = text.replace("foreach (var snapshot in _policyTable.ExportSnapshots())", "foreach (var snapshot in policyTable.ExportSnapshots())")

    text = replace_once(
        text,
        "                if (_runtime._context == null)\n"
        "                    _runtime._context = LayerBaseSynchronizationContext.Install();\n\n",
        "",
        "remove runtime synchronization context install",
    )
    text = replace_once(text, "                _runtime.EcsScheduler.Start();\n", "", "remove runtime ECS start")

    # RuntimeTimerSink belonged to the removed Runtime timer path.
    text = regex_once(
        text,
        r"\n    private sealed class RuntimeTimerSink : IExpiredTimerSink<ITimerAction>\n    \{.*?\n    \}\n",
        "\n",
        "remove runtime timer sink",
    )

    write(path, text)


def patch_layer_runtime_ecs() -> None:
    path = "LayerBase/Application/LayerRuntime.ECS.cs"
    write(
        path,
        """using Arch.Core;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Runtime.Query;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    // 下列属性仅是 MainScope 资源入口，不保存或释放独立 ECS 资源。
    internal World EcsWorld => MainScope.EcsWorld;

    internal EcsQueryRegistry EcsQueryRegistry => MainScope.EcsQueryRegistry;

    internal IEcsScheduler EcsScheduler =>
        MainScope.EcsScheduler ?? throw new InvalidOperationException("MainScope ECS scheduler is not initialized.");

    public EcsRuntimeOptions EcsOptions => ScopeHost == null ? _ecsOptions : MainScope.EcsOptions;

    internal void ConfigureEcs(EcsRuntimeOptions options)
    {
        if (BuildState != RuntimeBuildState.Created)
        {
            throw new InvalidOperationException("ECS mode must be configured before Build.");
        }

        _ecsOptions = options.Equals(default)
            ? EcsRuntimeOptions.Default
            : options;
    }

    internal IEcsWorkScheduler EcsWorkScheduler => (IEcsWorkScheduler)EcsScheduler;

    public void WaitEcsIdleForTest(TimeSpan timeout)
    {
        EcsWorkScheduler.WaitIdleForTest(timeout);
    }

    public long FlushEcsSubmissionsForTest()
    {
        return EcsWorkScheduler.FlushSubmissionsForTest();
    }

    public void WaitEcsFenceForTest(long fence, TimeSpan timeout)
    {
        EcsWorkScheduler.WaitFenceForTest(fence, timeout);
    }
}
""",
    )


def patch_scope_runtime() -> None:
    path = "LayerBase/Scope/ScopeRuntime.cs"
    text = read(path)

    text = replace_once(
        text,
        "    private readonly EventBuildPolicyTable _policyTable;\n",
        "    private EventBuildPolicyTable _policyTable;\n"
        "    private readonly PostIngressQueue _postIngress = new();\n",
        "add Scope-owned event policy and ingress",
    )
    text = replace_once(text, "        PostScheduler.BuildPlans(Array.Empty<PostTypePlan>());\n", "", "defer scope post plans")
    text = replace_once(
        text,
        "        DelayManager = DelayPublisherManager.Create(options.DelayBufferOptions, _policyTable);\n",
        "        DelayManager = DelayPublisherManager.Create(options.DelayBufferOptions, _policyTable);\n"
        "        _postIngress.SetCapacity(options.PostSchedulerOptions.MaxIngressQueueCapacity);\n"
        "        RebuildEventPolicies();\n",
        "initialize Scope event policies",
    )
    text = replace_once(
        text,
        "    public PostScheduler PostScheduler { get; }\n\n",
        "    public PostScheduler PostScheduler { get; }\n\n"
        "    internal EventBuildPolicyTable PolicyTable => _policyTable;\n\n",
        "expose Scope policy table",
    )

    insert_after_require_access = """    internal void RequireAccess(string apiName)
    {
        if (ReferenceEquals(ScopeExecution.Current.Runtime, this))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Scope '{Descriptor.Name}' local API '{apiName}' must be called from its owner scope execution context.");
    }
"""
    event_policy_methods = insert_after_require_access + """

    internal void RebuildEventPolicies()
    {
        var policyTable = new EventBuildPolicyTable(PostScheduler.Options.DefaultBackpressure);
        var plans = new List<PostTypePlan>();
        var metadata = LayerBase.Event.EventMetaData.EventMetaDataHandler.GetAllMetaData();

        foreach (var (_, meta) in metadata)
        {
            int eventId = meta.EventId;
            _ = meta.GetIdentity();

            EventPostPolicy? postPolicy = meta.GetPostPolicy();
            policyTable.SetMetaData(eventId, meta);
            if (postPolicy.HasValue) policyTable.SetPostPolicy(eventId, postPolicy.Value);

            EventTimerPolicy? timerPolicy = meta.GetTimerPolicy();
            if (timerPolicy.HasValue) policyTable.SetTimerPolicy(eventId, timerPolicy.Value);

            EventBufferPolicy? bufferPolicy = meta.GetBufferPolicy();
            if (bufferPolicy.HasValue) policyTable.SetBufferPolicy(eventId, bufferPolicy.Value);

            ActorMailOptions? actorMailOptions = meta.GetActorMailOptions();
            if (actorMailOptions.HasValue) policyTable.SetActorMailOptions(eventId, actorMailOptions.Value);

            EventPostPolicy effective = postPolicy
                ?? new EventPostPolicy(
                    PostDeliveryMode.Normal,
                    PostScheduler.Options.DefaultBackpressure,
                    0);
            plans.Add(new PostTypePlan(
                eventId,
                effective.Mode,
                effective.Backpressure,
                effective.MaxPending,
                PostScheduler.Options.DefaultBackpressure,
                effective.MergeFailure));
        }

        _policyTable = policyTable;
        PostScheduler.UpdatePolicyTable(policyTable);
        PostScheduler.BuildPlans(plans.ToArray());
        DelayManager.UpdatePolicyTable(policyTable);
    }
"""
    text = replace_once(text, insert_after_require_access, event_policy_methods, "add Scope event policy builder")

    try_post_block = """    public bool TryPost(ScopePostMessage message)
    {
        ThrowIfDisposed();
        ScopeRuntimeState state = State;
        if (!AcceptsBusinessIngress(state)) return false;
        if (_postDispatcher == null && state != ScopeRuntimeState.Created) return false;
        return _postInbox.TryEnqueue(message) == QueueEnqueueResult.Accepted;
    }
"""
    ingress_methods = try_post_block + """

    public void PostFromAnyThread<T>(in T value, EventPostPolicy? policy = default)
        where T : struct
    {
        _ = TryPostFromAnyThread(value, policy);
    }

    public bool TryPostFromAnyThread<T>(in T value, EventPostPolicy? policy = default)
        where T : struct
    {
        if (!AcceptsBusinessIngress(State)) return false;
        return _postIngress.Enqueue(value, policy);
    }
"""
    text = replace_once(text, try_post_block, ingress_methods, "add Scope-owned Post ingress API")

    text = replace_once(
        text,
        "            Capture(() => _subscriptionRegistry?.Dispose());\n",
        "            Capture(_postIngress.Clear);\n"
        "            Capture(() => _subscriptionRegistry?.Dispose());\n",
        "dispose Scope ingress",
    )

    text = replace_once(
        text,
        "        PostScheduler.Pump();\n\n"
        "        for (int i = 0; i < Services.Length; i++)\n",
        "        PostIngressDrainResult ingressResult = _postIngress.DrainTo(\n"
        "            PostScheduler,\n"
        "            PostScheduler.Options.MaxIngressPostsPerPump);\n"
        "        if (ingressResult.Failed > 0 && OwningRuntime?.IsDebugMode == true)\n"
        "        {\n"
        "            OwningRuntime.ReportWarning(\n"
        "                -1,\n"
        "                nameof(PostIngressQueue),\n"
        "                nameof(PostIngressQueue.DrainTo),\n"
        "                $\"PostFromAnyThread failed: {ingressResult.Failed}/{ingressResult.Drained}\");\n"
        "        }\n\n"
        "        PostScheduler.Pump();\n\n"
        "        for (int i = 0; i < Services.Length; i++)\n",
        "drain Scope ingress before post pump",
    )

    text = replace_once(
        text,
        "        for (int i = 0; i < Contexts.Length; i++)\n"
        "        {\n"
        "            if (Contexts[i] is LayerBase.DI.Options.IUpdate update)\n"
        "            {\n"
        "                update.Update();\n"
        "            }\n"
        "        }\n\n"
        "        PumpActors(deltaTime);\n",
        "        for (int i = 0; i < Contexts.Length; i++)\n"
        "        {\n"
        "            if (Contexts[i] is LayerBase.DI.Options.IUpdate update)\n"
        "            {\n"
        "                update.Update();\n"
        "            }\n"
        "        }\n\n"
        "        if (ScopeId == ScopeDescriptors.Main.ScopeId)\n"
        "        {\n"
        "            OwningRuntime?.PumpMainScopeLayers(deltaTime);\n"
        "        }\n\n"
        "        PumpActors(deltaTime);\n",
        "pump Layer chain inside MainScope execution",
    )

    fixed_method_anchor = """    private void ScheduleProjectedActorSweep()
    {
"""
    fixed_method = """    internal void PumpFixedServices(float fixedDeltaTime)
    {
        RequireAccess(nameof(PumpFixedServices));

        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is IFixedUpdate fixedUpdate)
            {
                fixedUpdate.FixedUpdate(fixedDeltaTime);
            }
        }

        for (int i = 0; i < Contexts.Length; i++)
        {
            if (Contexts[i] is IFixedUpdate fixedUpdate)
            {
                fixedUpdate.FixedUpdate(fixedDeltaTime);
            }
        }
    }

""" + fixed_method_anchor
    text = replace_once(text, fixed_method_anchor, fixed_method, "add Scope fixed lifecycle")

    text = replace_once(
        text,
        "            try\n"
        "            {\n"
        "                initializable.Initialize();\n"
        "            }\n",
        "            try\n"
        "            {\n"
        "                initializable.Initialize();\n"
        "                if (Services[i] is IPostBuild postBuild) postBuild.PostBuild();\n"
        "                if (Services[i] is IRuntimeStart runtimeStart) runtimeStart.RuntimeStart();\n"
        "            }\n",
        "extend Service start lifecycle",
    )
    text = replace_once(
        text,
        "            try\n"
        "            {\n"
        "                initializable.Initialize();\n"
        "            }\n",
        "            try\n"
        "            {\n"
        "                initializable.Initialize();\n"
        "                if (Contexts[i] is IPostBuild postBuild) postBuild.PostBuild();\n"
        "                if (Contexts[i] is IRuntimeStart runtimeStart) runtimeStart.RuntimeStart();\n"
        "            }\n",
        "extend Context start lifecycle",
    )

    text = replace_once(
        text,
        "        DisposeContexts();\n"
        "        DisposeServices();\n",
        "        StopContexts();\n"
        "        StopServices();\n"
        "        DisposeContexts();\n"
        "        DisposeServices();\n",
        "stop Scope objects before dispose",
    )

    dispose_services_anchor = """    private void DisposeServices()
    {
"""
    stop_methods = """    private void StopServices()
    {
        for (int i = Services.Length - 1; i >= 0; i--)
        {
            if (Services[i] is not IRuntimeStop runtimeStop) continue;
            try
            {
                runtimeStop.RuntimeStop();
            }
            catch (Exception ex)
            {
                ReportException(ex, i, LayerExceptionPhase.ServiceDispose, LayerQueueKind.None, -1);
            }
        }
    }

    private void StopContexts()
    {
        for (int i = Contexts.Length - 1; i >= 0; i--)
        {
            if (Contexts[i] is not IRuntimeStop runtimeStop) continue;
            try
            {
                runtimeStop.RuntimeStop();
            }
            catch (Exception ex)
            {
                ReportException(ex, -1, LayerExceptionPhase.ServiceDispose, LayerQueueKind.None, -1);
            }
        }
    }

""" + dispose_services_anchor
    text = replace_once(text, dispose_services_anchor, stop_methods, "add Scope stop lifecycle")

    text = replace_once(
        text,
        "        _postInbox.Close();\n"
        "        _callInbox.Close();\n",
        "        _postIngress.Clear();\n"
        "        _postInbox.Close();\n"
        "        _callInbox.Close();\n",
        "close Scope ingress",
    )

    write(path, text)


def patch_delay_manager() -> None:
    path = "LayerBase/Event/Delay/DelayPublisherManager.cs"
    text = read(path)
    anchor = """    public int RegisterPublisher(IDelayPublisherInternal publisher)
    {
"""
    replacement = """    internal void UpdatePolicyTable(EventBuildPolicyTable policyTable)
    {
        ThrowIfDisposed();
        PolicyTable = policyTable ?? throw new ArgumentNullException(nameof(policyTable));
    }

""" + anchor
    text = replace_once(text, anchor, replacement, "add Delay policy update")
    write(path, text)


def patch_layer_lifecycle() -> None:
    path = "LayerBase/Layer/Layer.cs"
    text = read(path)
    text = replace_once(
        text,
        "            if (resolved.Instance is IService scopedService &&\n"
        "                ScopeRuntimePlanner.IsScopedServiceType(scopedService.GetType()))\n"
        "            {\n"
        "                continue;\n"
        "            }\n",
        "            // ScopeHost 已在 LifecycleBuild 前完成绑定。所有业务 Service 的\n"
        "            // 初始化、更新、停止和释放统一由 OwnerScope 管理。\n"
        "            if (resolved.Instance is IService scopeOwnedService &&\n"
        "                ScopeObjectBinder.TryGet(scopeOwnedService, out _))\n"
        "            {\n"
        "                continue;\n"
        "            }\n",
        "transfer Service lifecycle to Scope",
    )
    write(path, text)


def patch_legacy_di_disposal() -> None:
    path = "LayerBase/DI/ServiceProvider.cs"
    text = read(path)
    if "using LayerBase.Scope;" not in text:
        text = replace_once(text, "using LayerBase.Layers;\n", "using LayerBase.Layers;\nusing LayerBase.Scope;\n", "add Scope using")
    text = replace_once(
        text,
        "            if (!lazy.IsValueCreated) continue;\n"
        "            if (lazy.Value is IDisposable disposable)\n"
        "                disposable.Dispose();\n",
        "            if (!lazy.IsValueCreated) continue;\n"
        "            object instance = lazy.Value;\n"
        "            if (ScopeObjectBinder.TryGet(instance, out _)) continue;\n"
        "            if (instance is IDisposable disposable)\n"
        "                disposable.Dispose();\n",
        "prevent Layer provider double-dispose",
    )
    write(path, text)

    path = "LayerBase/DI/WorldServiceRoot.cs"
    text = read(path)
    if "using LayerBase.Scope;" not in text:
        text = replace_once(text, "using System.Collections.Concurrent;\n", "using System.Collections.Concurrent;\nusing LayerBase.Scope;\n", "add Scope using to world root")
    text = replace_once(
        text,
        "            if (lazy.Value is IDisposable disposable)\n"
        "            {\n"
        "                disposable.Dispose();\n"
        "            }\n",
        "            object instance = lazy.Value;\n"
        "            if (ScopeObjectBinder.TryGet(instance, out _))\n"
        "            {\n"
        "                continue;\n"
        "            }\n\n"
        "            if (instance is IDisposable disposable)\n"
        "            {\n"
        "                disposable.Dispose();\n"
        "            }\n",
        "prevent world root double-dispose",
    )
    write(path, text)


def patch_module_main_scope() -> None:
    path = "LayerBase/Modules/ModuleRuntimeBuilder.cs"
    text = read(path)

    text = replace_once(
        text,
        "        var scopeDefinitions = new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>();\n",
        "        var scopeDefinitions = new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>();\n",
        "module builder anchor",
    )

    allocate = """    private static IReadOnlyDictionary<RuntimeTypeHandle, int> AllocateScopeIds(
        IEnumerable<ScopeDefinitionContribution> scopeDefinitions)
    {
        int nextScopeId = 1;

        return scopeDefinitions
               .OrderBy(static scope => GetTypeName(scope.ScopeType), StringComparer.Ordinal)
               .Select(scope => new { scope.ScopeType, ScopeId = nextScopeId++ })
               .ToDictionary(static item => item.ScopeType, static item => item.ScopeId);
    }
"""
    allocate_new = """    private static IReadOnlyDictionary<RuntimeTypeHandle, int> AllocateScopeIds(
        IEnumerable<ScopeDefinitionContribution> scopeDefinitions)
    {
        int nextScopeId = 1;
        var result = new Dictionary<RuntimeTypeHandle, int>
        {
            [typeof(MainScope).TypeHandle] = ScopeDescriptors.Main.ScopeId
        };

        foreach (ScopeDefinitionContribution scope in scopeDefinitions
                     .Where(static scope => !scope.ScopeType.Equals(typeof(MainScope).TypeHandle))
                     .OrderBy(static scope => GetTypeName(scope.ScopeType), StringComparer.Ordinal))
        {
            result.Add(scope.ScopeType, nextScopeId++);
        }

        return result;
    }
"""
    text = replace_once(text, allocate, allocate_new, "reserve MainScope id zero")

    text = replace_once(
        text,
        "            if (!scopeDefinitions.ContainsKey(service.OwnerScopeType))\n"
        "            {\n"
        "                throw new ModuleBuildException(\n"
        "                    ModuleBuildErrorCodes.MissingScopeDefinition,\n"
        "                    $\"Service '{GetTypeName(service.ServiceType)}' targets Scope '{GetTypeName(service.OwnerScopeType)}', but no installed Module defines that Scope.\");\n"
        "            }\n",
        "            bool targetsBuiltInMainScope = service.OwnerScopeType.Equals(typeof(MainScope).TypeHandle);\n"
        "            if (!targetsBuiltInMainScope && !scopeDefinitions.ContainsKey(service.OwnerScopeType))\n"
        "            {\n"
        "                throw new ModuleBuildException(\n"
        "                    ModuleBuildErrorCodes.MissingScopeDefinition,\n"
        "                    $\"Service '{GetTypeName(service.ServiceType)}' targets Scope '{GetTypeName(service.OwnerScopeType)}', but no installed Module defines that Scope.\");\n"
        "            }\n",
        "allow built-in MainScope services",
    )
    write(path, text)

    path = "LayerBase/Scope/ScopeCompositionBuilder.cs"
    text = read(path)
    text = replace_once(
        text,
        "        int maxScopeId = scopeDefinitions.Count > 0\n"
        "            ? scopeDefinitions.Values.Max(d => RequireScopeId(scopeIds, d.ScopeType))\n"
        "            : 0;\n",
        "        int maxScopeId = scopeIds.Count > 0 ? scopeIds.Values.Max() : 0;\n",
        "include MainScope in composition range",
    )
    text = replace_once(
        text,
        "        var scopes = new ScopePlan[maxScopeId + 1];\n"
        "        scopes[0] = new ScopePlan(\n"
        "            ScopeDescriptors.Main,\n"
        "            typeof(MainScope),\n"
        "            Array.Empty<ScopeServicePlan>(),\n"
        "            Array.Empty<ScopeContextPlan>(),\n"
        "            ScopeResourcePlan.Empty);\n\n"
        "        for (int scopeId = 1; scopeId <= maxScopeId; scopeId++)\n",
        "        var scopes = new ScopePlan[maxScopeId + 1];\n"
        "        ScopeServicePlan[] mainServices = scopeServicesById[0] ?? Array.Empty<ScopeServicePlan>();\n"
        "        ScopeContextPlan[] mainContexts = scopeContextsById[0]?.ToArray() ?? Array.Empty<ScopeContextPlan>();\n"
        "        resourcePlansById[0] = BuildResourcePlan(\n"
        "            mainServices,\n"
        "            mainContexts,\n"
        "            catalog.ResourceExports,\n"
        "            catalog.ResourceImports);\n"
        "        scopes[0] = new ScopePlan(\n"
        "            ScopeDescriptors.Main,\n"
        "            typeof(MainScope),\n"
        "            mainServices,\n"
        "            mainContexts,\n"
        "            resourcePlansById[0]);\n\n"
        "        for (int scopeId = 1; scopeId <= maxScopeId; scopeId++)\n",
        "compose MainScope services and contexts",
    )
    write(path, text)


def add_tests() -> None:
    path = ROOT / "LayerBase.Test/MainScopeOwnershipTests.cs"
    path.write_text(
        """using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class MainScopeOwnershipTests
{
    [SetUp]
    public void SetUp() => LayerHub.Reset();

    [TearDown]
    public void TearDown() => LayerHub.Reset();

    [Test]
    public void Runtime_without_explicit_scope_must_create_MainScope()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new EmptyLayer())
            .Build();

        Assert.That(runtime.ScopeHost, Is.Not.Null);
        Assert.That(runtime.ScopeHost!.Scopes, Has.Count.EqualTo(1));
        Assert.That(runtime.MainScope.ScopeId, Is.EqualTo(0));
        Assert.That(runtime.MainScope.Descriptor.Name, Is.EqualTo("MainScope"));
    }

    [Test]
    public void Unannotated_service_must_be_owned_and_updated_by_MainScope_once()
    {
        var service = new MainProbeService();
        var layer = new ServiceLayer(service);
        using LayerRuntime runtime = LayerHub.CreateLayers().Push(layer).Build();

        ScopeObjectBinding binding = ScopeObjectBinder.Require(service);
        Assert.That(binding.Scope, Is.SameAs(runtime.MainScope));
        Assert.That(service.InitializeCount, Is.EqualTo(1));

        runtime.Pump(0.016f);

        Assert.That(service.UpdateCount, Is.EqualTo(1));
        Assert.That(service.LastScopeId, Is.EqualTo(0));
    }

    [Test]
    public void Layer_update_must_execute_inside_MainScope()
    {
        var layer = new ScopeAwareLayer();
        using LayerRuntime runtime = LayerHub.CreateLayers().Push(layer).Build();

        runtime.Pump(0.016f);

        Assert.That(layer.PumpCount, Is.EqualTo(1));
        Assert.That(layer.LastScopeId, Is.EqualTo(0));
    }

    [Test]
    public void Runtime_resource_entry_points_must_route_to_MainScope_instances()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers().Push(new EmptyLayer()).Build();

        Assert.That(runtime.EventCenter, Is.SameAs(runtime.MainScope.EventCenter));
        Assert.That(runtime.Scheduler, Is.SameAs(runtime.MainScope.PostScheduler));
        Assert.That(runtime.Timer, Is.SameAs(runtime.MainScope.Timer));
        Assert.That(runtime.DelayManager, Is.SameAs(runtime.MainScope.DelayManager));
        Assert.That(runtime.EcsWorld, Is.SameAs(runtime.MainScope.EcsWorld));
        Assert.That(runtime.EcsScheduler, Is.SameAs(runtime.MainScope.EcsScheduler));
    }

    [Test]
    public void Module_builder_must_reserve_MainScope_id_zero_without_definition()
    {
        using var module = new MainScopeModule();

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(module);

        Assert.That(catalog.ScopeIds[typeof(MainScope).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.Services, Has.Count.EqualTo(1));

        ScopeCompositionPlan plan = ScopeCompositionBuilder.Build(catalog);
        Assert.That(plan.Scopes, Has.Length.EqualTo(1));
        Assert.That(plan.Scopes[0].Descriptor.ScopeId, Is.EqualTo(0));
        Assert.That(plan.Scopes[0].Services, Has.Length.EqualTo(1));
    }

    private sealed class EmptyLayer : Layer
    {
    }

    private sealed class ServiceLayer : Layer
    {
        private readonly MainProbeService _service;

        public ServiceLayer(MainProbeService service)
        {
            _service = service;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_service);
        }
    }

    private sealed class ScopeAwareLayer : Layer
    {
        public int PumpCount { get; private set; }
        public int LastScopeId { get; private set; } = -1;

        public override void Pump(float deltaTime)
        {
            PumpCount++;
            LastScopeId = ScopeExecution.Current.ScopeId;
        }
    }

    private sealed class MainProbeService : IService, IInitializable, IUpdate
    {
        public int InitializeCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int LastScopeId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            InitializeCount++;
            LastScopeId = ScopeExecution.Current.ScopeId;
        }

        public void Update()
        {
            UpdateCount++;
            LastScopeId = ScopeExecution.Current.ScopeId;
        }
    }

    private sealed class MainScopeModule : ILayerBaseModule, IDisposable
    {
        private readonly MainModuleService _service = new();

        public ModuleManifest Manifest => new(
            layerContracts: Array.Empty<LayerContractContribution>(),
            scopeDefinitions: Array.Empty<ScopeDefinitionContribution>(),
            messageContracts: Array.Empty<ScopeMessageContractContribution>(),
            services:
            [
                new ServiceContribution(
                    typeof(MainModuleService).TypeHandle,
                    Array.Empty<RuntimeTypeHandle>(),
                    typeof(MainScope).TypeHandle,
                    () => _service,
                    static (_, _, _) => { },
                    moduleLocalServiceId: 0)
            ],
            contexts: Array.Empty<ContextContribution>(),
            handlers: Array.Empty<ScopeHandlerContribution>(),
            resourceExports: Array.Empty<LayerBase.Scope.Resources.ScopeResourceExportContribution>(),
            resourceImports: Array.Empty<LayerBase.Scope.Resources.ScopeResourceImportContribution>());

        ModuleManifest ILayerBaseModule.Manifest => Manifest;

        public void Dispose()
        {
        }
    }

    private sealed class MainModuleService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
""",
        encoding="utf-8",
        newline="\n",
    )


def main() -> None:
    patch_layer_runtime()
    patch_layer_runtime_ecs()
    patch_scope_runtime()
    patch_delay_manager()
    patch_layer_lifecycle()
    patch_legacy_di_disposal()
    patch_module_main_scope()
    add_tests()


if __name__ == "__main__":
    main()
