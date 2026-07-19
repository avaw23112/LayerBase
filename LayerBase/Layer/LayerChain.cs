using System.Text;
using System.Threading;
using LayerBase.Async;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase.Layers;

/// <summary>
/// 管理 Layer 链的内部实现。负责 Layer 的索引分配和生命周期构建。
/// </summary>
internal sealed class LayerChain
{
    private readonly ResponsibilityChain _responsibilityChain;
    private readonly LayerRuntime _owner;
    private Layer?[] _indexedLayers = Array.Empty<Layer?>();
    private ulong _hasDelayMask;
    private bool _delayDirty;

    /// <summary>
    /// 是否有任何 Layer 包含活跃的延迟发布器。
    /// </summary>
    public bool HasAnyDelay
    {
        get
        {
            if (_delayDirty) RebuildDelayMask();
            return _hasDelayMask != 0;
        }
    }

    /// <summary>
    /// 标记延迟掩码需要重建。
    /// </summary>
    internal void MarkDelayDirty()
    {
        _delayDirty = true;
    }

    /// <summary>
    /// 重新计算包含延迟发布器的 Layer 位掩码。
    /// </summary>
    private void RebuildDelayMask()
    {
        _hasDelayMask = 0;
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer && layer.HasDelayPublisher)
                _hasDelayMask |= 1UL << layer.RouteIndex;
        }
        _delayDirty = false;
    }

    internal LayerChain(ResponsibilityChain chain, LayerRuntime owner)
    {
        _responsibilityChain = chain;
        _owner = owner;
    }

    /// <summary>
    /// 遍历所有已索引的 Layer。
    /// </summary>
    internal IEnumerable<Layer> GetNodes()
    {
        for (var i = 0; i < _indexedLayers.Length; i++)
            if (_indexedLayers[i] != null)
                yield return _indexedLayers[i]!;
    }

    internal void AssignLayerIndexes()
    {
        AssignEventBus();
    }

    /// <summary>
    /// 向责任链末尾添加一个节点。
    /// </summary>
    internal void AddNode(Node node)
    {
        _responsibilityChain.AddLast(node);
    }

    internal void DisposeScopeServices(int scopeId)
    {
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer)
                layer.DisposeScopeServices(scopeId);
        }
    }

    private static void RunScopeLifecycleOnOwnerThread(
        IReadOnlyList<ScopeRuntime> scopes,
        ScopeLifecyclePhase phase,
        in ShutdownDeadline deadline)
    {
        for (int i = scopes.Count - 1; i >= 0; i--)
        {
            ScopeRuntime scope = scopes[i];

            if (scope.Options.Threading != ScopeThreadingMode.Worker)
            {
                RunInlineLifecycle(scope, phase);
                continue;
            }

            switch (phase)
            {
                case ScopeLifecyclePhase.Initialize:
                {
                    ScopeInitializeResponse response =
                        ScopeControlBarrier.Wait(
                            scope.RequestInitializeAsync(),
                            in deadline,
                            $"{scope.Descriptor.Name}.Initialize");

                    ScopeControlBarrier.EnsureSucceeded(
                        response.Result,
                        "Initialize",
                        scope);
                    break;
                }

                case ScopeLifecyclePhase.PostBuild:
                {
                    ScopePostBuildResponse response =
                        ScopeControlBarrier.Wait(
                            scope.RequestPostBuildAsync(),
                            in deadline,
                            $"{scope.Descriptor.Name}.PostBuild");

                    ScopeControlBarrier.EnsureSucceeded(
                        response.Result,
                        "PostBuild",
                        scope);
                    break;
                }

                case ScopeLifecyclePhase.RuntimeStart:
                {
                    ScopeRuntimeStartResponse response =
                        ScopeControlBarrier.Wait(
                            scope.RequestRuntimeStartAsync(),
                            in deadline,
                            $"{scope.Descriptor.Name}.RuntimeStart");

                    ScopeControlBarrier.EnsureSucceeded(
                        response.Result,
                        "RuntimeStart",
                        scope);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase),
                        phase,
                        "Unsupported build lifecycle phase.");
            }
        }
    }

    private static void RunInlineLifecycle(
        ScopeRuntime scope,
        ScopeLifecyclePhase phase)
    {
        switch (phase)
        {
            case ScopeLifecyclePhase.Initialize:
                scope.LifecyclePlan.RunInitialize();
                break;

            case ScopeLifecyclePhase.PostBuild:
                scope.LifecyclePlan.RunPostBuild();
                break;

            case ScopeLifecyclePhase.RuntimeStart:
                scope.LifecyclePlan.RunRuntimeStart();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(phase),
                    phase,
                    "Unsupported build lifecycle phase.");
        }
    }

    /// <summary>
    /// 预构建阶段：分配事件总线索引、准备构建和自动绑定。
    /// </summary>
    internal void Prebuild()
    {
        AssignEventBus();
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer)
            {
                layer.PrepareBuild();
                layer.BuildAutoBinding();
            }
        }
    }

    /// <summary>
    /// 完整构建阶段：绑定共享字段、执行生命周期构建、构建 lifecycle plan。
    /// </summary>
    internal void BuildPlans(int eventStateSlabSize, bool releaseMode)
    {
        var builtLayers = new List<Layer>();
        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
                builtLayers.Add(layer);

        SharedFieldBinder.Bind(
            builtLayers.SelectMany(static layer => layer.GetSharedFieldParticipants()));

        _hasDelayMask = 0;
        foreach (var layer in builtLayers)
        {
            layer.LifecycleBuild();
            if (layer.HasDelayPublisher) _hasDelayMask |= 1UL << layer.RouteIndex;
        }

        var compositionScopes = _owner.CompositionPlan.Scopes;
        for (int i = 0; i < compositionScopes.Length; i++)
        {
            var scopePlan = compositionScopes[i];
            int scopeId = scopePlan.Descriptor.ScopeId;
            var lifecyclePlan = ScopeLifecyclePlan.Build(builtLayers, scopeId);
            if (_owner.ScopeHost.TryGetRuntime(scopeId, out var scopeRuntime))
                scopeRuntime.SetLifecyclePlan(lifecyclePlan);
        }

        EventGraphValidator.Validate(builtLayers, _owner);
    }

    /// <summary>
    /// 在 Worker 启动后执行生命周期阶段。
    /// </summary>
    internal void RunLifecyclePhases(
        in ShutdownDeadline deadline,
        Action? afterPostBuild = null)
    {
        RunScopeLifecycleOnOwnerThread(
            _owner.ScopeHost.Scopes,
            ScopeLifecyclePhase.Initialize,
            in deadline);

        RunScopeLifecycleOnOwnerThread(
            _owner.ScopeHost.Scopes,
            ScopeLifecyclePhase.PostBuild,
            in deadline);

        afterPostBuild?.Invoke();

        RunScopeLifecycleOnOwnerThread(
            _owner.ScopeHost.Scopes,
            ScopeLifecyclePhase.RuntimeStart,
            in deadline);
    }

    /// <summary>
    /// 为每个 Layer 分配路由索引并建立索引数组。
    /// </summary>
    private void AssignEventBus()
    {
        var maxIndex = -1;
        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
            {
                if (layer.RouteIndex == -1)
                {
                    var index = _owner.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                }
                if (layer.RouteIndex > maxIndex) maxIndex = layer.RouteIndex;
            }

        if (maxIndex != -1)
        {
            _indexedLayers = new Layer?[maxIndex + 1];
            foreach (var node in _responsibilityChain)
                if (node is Layer layer)
                    _indexedLayers[layer.RouteIndex] = layer;
        }
    }

    /// <summary>
    /// 生成拓扑审计摘要文本。
    /// </summary>
    internal string GetTopologySummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("LayerBase Topology Audit Report");
        sb.AppendLine("------------------------------------------------");
        for (var i = 0; i < _indexedLayers.Length; i++)
        {
            var layer = _indexedLayers[i];
            if (layer == null) continue;

            sb.AppendLine($"Layer {i}: {layer.GetType().Name} [{(layer.HasActiveLogic ? "Active" : "Passive")}]");
            foreach (var sub in layer.DiscoveredSubscribers)
            {
                sb.AppendLine($"  -> [M] {sub.GetType().Name}");
                var subs = sub.GetSubscribedEvents().ToList();
                if (subs.Count > 0)
                    sb.AppendLine($"       |-- Subscribed: {string.Join(", ", subs.Select(t => t.Name))}");

                var deps = sub.GetEventDependencies().ToList();
                if (deps.Count > 0)
                    sb.AppendLine($"       |-- Dispatched: {string.Join(", ", deps.Select(d => d.Target.Name))}");
            }
        }
        sb.AppendLine("------------------------------------------------");
        return sb.ToString();
    }
}
