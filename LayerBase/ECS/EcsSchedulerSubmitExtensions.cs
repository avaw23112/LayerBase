using Arch.Core;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS;

public static class EcsSchedulerSubmitExtensions
{
    private delegate void BringQueryExecute<TJob>(
        World world,
        ArchQuery query,
        object? predicate,
        ref TJob job)
        where TJob : struct;

    public static void SubmitBringQuery<TEvent0, TJob, T0>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob1x1<T0, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor1<T0>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob2x1<T0, T1, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor2<T0, T1>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor3<T0, T1, T2>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2>(this IEcsScheduler scheduler, int queryId, int predicateId, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
    {
        SubmitBringQuery<TEvent0, TJob, T0, T1, T2>(
            scheduler,
            queryId,
            (object?)null,
            in job);
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor4<T0, T1, T2, T3>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor5<T0, T1, T2, T3, T4>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6, T7>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob9x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob10x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob11x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>?)predicate, ref job));
    }

    public static void SubmitBringQuery<TEvent0, TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob12x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
    {
        SubmitBringQueryCore(scheduler, queryId, predicate, in job,
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Post<TEvent0, TJob>(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>?)predicate, ref job));
    }

    private static void SubmitBringQueryCore<TJob>(
        IEcsScheduler scheduler,
        int queryId,
        object? predicate,
        in TJob job,
        BringQueryExecute<TJob> execute)
        where TJob : struct
    {
        switch (scheduler)
        {
            case AsyncEcsScheduler asyncScheduler:
                SubmitBring(asyncScheduler.Runtime, asyncScheduler.World, queryId, predicate, in job, execute);
                return;
            case SyncEcsScheduler syncScheduler:
                SubmitBring(syncScheduler.Runtime, syncScheduler.World, queryId, predicate, in job, execute);
                return;
            default:
                throw new NotSupportedException($"Unsupported ECS scheduler type '{scheduler.GetType().FullName}'.");
        }
    }

    private static void SubmitBring<TJob>(
        LayerRuntime runtime,
        World world,
        int queryId,
        object? predicate,
        in TJob job,
        BringQueryExecute<TJob> execute)
        where TJob : struct
    {
        ArchQuery query = runtime.EcsQueryRegistry.Get(queryId);
        TJob localJob = job;
        execute(world, query, predicate, ref localJob);
    }
}
