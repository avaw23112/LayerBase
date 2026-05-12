using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Actor;

public static class ActorExtensions
{
    public static ActorId GetActorId(this IActor actor)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).GetId();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime.Post(in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostLastest<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime.PostLatest(in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PostCoalesced<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime.PostCoalesced(in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime.Send(in value);
    }

    public static TimerHandle SchedulePost<TValue>(
        this IActor         actor,
        in   TValue         value,
        float               delaySeconds,
        EventPostPolicy?    expiredPostPolicy = default,
        int                 repeatCount       = 0,
        float               intervalSeconds   = 0,
        TimerRepeatMode?    repeatMode        = default,
        TimerCatchUpPolicy? catchUpPolicy     = default)
        where TValue : struct
    {
        var runtime = ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime;
        var eventId = EventTypeId<TValue>.Id;
        var timerPolicy = runtime.PolicyTable.GetTimerPolicy(eventId);

        return runtime.Timer.Schedule(
            new PostEventAction<TValue>(
                value,
                expiredPostPolicy ?? timerPolicy?.ExpiredPostPolicy),
            delaySeconds,
            repeatCount: repeatCount,
            intervalSeconds: intervalSeconds,
            repeatMode: repeatMode ?? timerPolicy?.RepeatMode,
            catchUpPolicy: catchUpPolicy ?? timerPolicy?.CatchUpPolicy);
    }

    public static LBTask<TResponse> CallAsync<TLayer, TRequest, TResponse>(this IActor actor, TRequest request,
                                                                           CancellationToken cancellationToken =
                                                                               default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Context.Runtime
                                   .CallAsync<TLayer, TRequest, TResponse>(request);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult PostInside<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Context.PostInside(in value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetEnable(this IActor actor)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Context.IsEnable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetEnable(this IActor actor, bool enable)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Context.SetEnable(enable);
    }
}