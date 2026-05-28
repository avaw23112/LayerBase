using System.Runtime.CompilerServices;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal ProjectedActorHandle CreateProjectedActor<TActor>()
        where TActor : class, IPooledActor, new()
    {
        TActor actor = CreateActor<TActor>(usePool: true);
        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);

        return new ProjectedActorHandle(
            generated.GetId(),
            actor);
    }

    internal bool TryGetActor(
        ActorId     actorId,
        out IActor? actor)
    {
        actor = null;

        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        BehaviourArchetype? archetype = _archetypes[actorId.ArchetypeId];
        if (archetype == null)
        {
            return false;
        }

        return archetype.TryGetActor(
            actorId,
            out actor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetPooledActor(
        ActorId          actorId,
        out IPooledActor pooledActor)
    {
        if (!TryGetActor(
                actorId,
                out IActor? actor))
        {
            pooledActor = null!;
            return false;
        }

        pooledActor = actor as IPooledActor;
        return pooledActor != null;
    }

    internal bool ReleaseProjectedActor(
        ActorId                     actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        BehaviourArchetype? archetype = _archetypes[actorId.ArchetypeId];
        if (archetype == null)
        {
            return false;
        }

        return archetype.ReleaseProjectedActor(
            actorId,
            this,
            releasePolicy);
    }

    /// <summary>
    /// 检查 ProjectedActor 是否处于 Disabled 状态。
    /// </summary>
    internal bool IsProjectedActorDisabled(ActorId actorId)
    {
        return !IsEnable(actorId);
    }

    /// <summary>
    /// 将 ProjectedActor 从 Disabled 恢复为 Active。
    ///
    /// 返回值：
    /// true 表示 Enable 成功。
    /// false 表示 Actor 不存在或 Enable 失败。
    /// </summary>
    internal bool EnableProjectedActorIfDisabled(ActorId actorId)
    {
        if (!TryGetPooledActor(actorId, out IPooledActor actor))
        {
            return false;
        }

        SetEnable(actorId, true);
        actor.OnEnable();
        return true;
    }

    /// <summary>
    /// 将 ProjectedActor 从 Active 进入 Disabled 状态。
    ///
    /// 返回值：
    /// true 表示 Disable 成功。
    /// false 表示 Actor 不存在或已经 Disabled。
    /// </summary>
    internal bool DisableProjectedActor(ActorId actorId)
    {
        if (!TryGetPooledActor(actorId, out IPooledActor actor))
        {
            return false;
        }

        if (IsProjectedActorDisabled(actorId))
        {
            return true;
        }

        actor.OnDisable();
        SetEnable(actorId, false);
        return true;
    }
}
