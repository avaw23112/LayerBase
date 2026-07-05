using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly List<ActorCallEntry> _callEntries = new();
    private readonly List<ActorLifecycleMethodMeta> _lifecycleMethods = new();
    private readonly HashSet<int> _eventIds = new();
    private readonly HashSet<int> _callRouteIds = new();
    private readonly HashSet<int> _tagIds = new();
    private readonly HashSet<int> _groupIds = new();

    /// <summary>
    /// 添加行为处理器工厂。
    ///
    /// 作用：
    /// 接收 handler factory，在 Actor 创建时将实例方法绑定为事件处理委托。
    /// 生成器会生成 static (TActor actor) => actor.Method 形式的工厂。
    /// </summary>
    /// <typeparam name="TActor">
    /// Actor 类型。
    /// </typeparam>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <param name="handlerFactory">
    /// 处理器工厂委托。
    /// </param>
    public void AddBehaviour<TActor, TEvent>(
        ActorBehaviourHandlerFactory<TActor, TEvent> handlerFactory)
        where TActor : class, IActor
        where TEvent : struct
    {
        if (handlerFactory == null)
        {
            throw new ArgumentNullException(nameof(handlerFactory));
        }

        int eventTypeId = EventTypeId<TEvent>.Id;
        if (!_eventIds.Add(eventTypeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has behaviour for event {typeof(TEvent).Name}.");
        }

        // 创建类型化的注册委托，避免运行时反射
        ActorStreamHandlerRegister streamRegister = (actor, archetypeId, slotIndex, generation, world) =>
        {
            var typedActor = (TActor)actor;
            ActorEventHandler<TEvent> handler = handlerFactory(typedActor);

            // 确保 EventStreamCenter 存在（per-archetype）
            EventStreamCenter<TEvent>? center =
                EventStreamRuntime<TEvent>.GetCenterUnchecked(world.RuntimeIndex, archetypeId);

            if (center == null)
            {
                // 创建 EventStream 运行时
                ActorEventStreamPlan<TEvent> plan = ActorEventStreamPlanBuilder.Build<TEvent>();
                world.GetOrCreateEventStreamRuntime<TEvent>(plan, archetypeId);
                center = EventStreamRuntime<TEvent>.GetCenterUnchecked(world.RuntimeIndex, archetypeId);
            }

            if (center != null)
            {
                center.RegisterHandler(slotIndex, generation, handler);
            }
        };

        // 创建类型化的注销委托，避免运行时遍历全部 EventStreamRuntime
        ActorStreamHandlerUnregister streamUnregister =
            static (archetypeId, slotIndex, world) =>
            {
                EventStreamCenter<TEvent>? center =
                    EventStreamRuntime<TEvent>.GetCenterUnchecked(
                        world.RuntimeIndex,
                        archetypeId);

                center?.UnregisterHandler(slotIndex);
            };

        _entries.Add(new ActorBehaviourEntry(
            eventTypeId,
            typeof(TEvent),
            handlerFactory,
            streamRegister,
            streamUnregister));
    }

    public void AddCallBehaviour<TActor, TRequest, TResponse>(
        ActorCallInvoker<TActor, TRequest, TResponse> invoker)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        if (invoker == null)
        {
            throw new ArgumentNullException(nameof(invoker));
        }

        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        if (!_callRouteIds.Add(routeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has call behaviour for request {typeof(TRequest).Name} and response {typeof(TResponse).Name}.");
        }

        _callEntries.Add(new ActorCallEntry(
            routeId,
            typeof(TRequest),
            typeof(TResponse),
            invoker,
            static (storage, rawInvoker, world) =>
            {
                var typedStorage = (TypedActorStorage<TActor>)storage;
                var typedInvoker = (ActorCallInvoker<TActor, TRequest, TResponse>)rawInvoker;
                return typedStorage.BuildCallColumnDirect(world, typedInvoker);
            }));
    }

    public void AddTag<TTag>()
        where TTag : struct, IActorTag
    {
        _tagIds.Add(ActorTagId<TTag>.Id);
    }

    public void AddGroup<TGroup>()
        where TGroup : struct, IActorGroup
    {
        _groupIds.Add(ActorGroupId<TGroup>.Id);
    }

    public void AddLifecycleMethod(
        ActorLifecyclePhase         phase,
        TickTier                    tier,
        int                         tickPhase,
        ActorLifecycleMethodInvoker invoker)
    {
        if (invoker == null)
        {
            throw new ArgumentNullException(nameof(invoker));
        }

        _lifecycleMethods.Add(new ActorLifecycleMethodMeta(
            phase,
            tier,
            tickPhase,
            invoker));
    }

    internal ActorTypeMeta<TActor> Build<TActor>()
        where TActor : class, IActor
    {
        ActorBehaviourEntry[] entries = _entries
                                        .OrderBy(static entry => entry.EventTypeId)
                                        .ToArray();

        ActorCallEntry[] callEntries = _callEntries
                                       .OrderBy(static entry => entry.RouteId)
                                       .ToArray();

        int[] eventTypeIds = entries
                             .Select(static entry => entry.EventTypeId)
                             .ToArray();

        int[] tagIds = _tagIds
                       .OrderBy(static id => id)
                       .ToArray();

        int[] groupIds = _groupIds
                         .OrderBy(static id => id)
                         .ToArray();

        ActorLifecycleMethodMeta[] lifecycleMethods = _lifecycleMethods.ToArray();

        return new ActorTypeMeta<TActor>(
            new BehaviourSignature(eventTypeIds),
            entries,
            callEntries,
            lifecycleMethods,
            tagIds,
            groupIds);
    }
}
