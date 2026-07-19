using LayerBase.Core.Event;
using LayerBase.Async;
using System.Text;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

internal abstract class TypedStorageRuntime
{
    internal abstract bool TryGetActor(
        ActorId     actorId,
        out IActor? actor);

    internal abstract bool ReleaseProjectedActor(
        ActorId                     actorId,
        ActorWorld                  world,
        ProjectedActorReleasePolicy releasePolicy);

    public abstract bool IsAlive(int slotIndex, int generation);

    public abstract ActorSlotState GetSlotState(int slotIndex);

    public abstract int GetGeneration(int slotIndex);

    public abstract bool IsEnable(int slotIndex, int generation);

    public abstract bool SetEnable(int slotIndex, int generation, bool enable);

    public abstract bool MarkPendingDestroy(int slotIndex, int generation);

    public abstract int MarkAllPendingDestroy();

    public abstract void SweepPendingDestroy(ActorWorld world);

    public abstract int CountActiveOperations();

    public abstract bool IsLifecycleRunnable(int slotIndex, int generation);

    public abstract void PostAll<TEvent>(
        ActorWorld world,
        in TEvent  value)
        where TEvent : struct;

    public abstract bool IsCurrentGeneration(ActorId actorId);

    // Post<TEvent>: removed. Use ActorWorld.PostTo through compiled route.
    // PostToAliveActors: removed. Use PostAll through compiled route.
    // PostManyToAliveActors: removed. Use PostAll through compiled route.

    public abstract DispatchResult DispatchNow<TEvent>(
        int       slotIndex,
        int       generation,
        in TEvent value)
        where TEvent : struct;

    public abstract LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        int               slotIndex,
        int               generation,
        in TRequest       request,
        CancellationToken cancellationToken)
        where TRequest : struct
        where TResponse : struct;

    public abstract PostResult PostCall<TRequest, TResponse>(
        int                                   slotIndex,
        in ActorCallMail<TRequest, TResponse> mail)
        where TRequest : struct
        where TResponse : struct;

    public abstract IEnumerable<IActor> EnumerateActors();

    public abstract int CountAlive();

    public abstract int CountEnabled();

    public abstract int CountPendingDestroy();

    public abstract bool HasAnyAlive();

    public abstract int GetTotalPendingMailCount();

    public abstract string ActorTypeName { get; }

    public abstract ActorDebugInfo GetDebugInfo(ActorId actorId, string archetypeInfo);

    public abstract void AppendDebugRow(StringBuilder builder, int archetypeId, string archetypeInfo);
}
