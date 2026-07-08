using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS;

public static class EcsSchedulerSubmitExtensions
{
    private delegate void PlainQueryExecute<TJob>(
        World world,
        ArchQuery query,
        object? predicate,
        ref TJob job)
        where TJob : struct;

    private delegate void BringQueryExecute<TJob>(
        World world,
        ArchQuery query,
        object? predicate,
        ref TJob job)
        where TJob : struct;

    public static void SubmitPlainQuery<TJob, T0>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor1<T0>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor1<T0>.ForEach(world, query, (ProjectionPredicate<T0>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor2<T0, T1>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor2<T0, T1>.ForEach(world, query, (ProjectionPredicate<T0, T1>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor3<T0, T1, T2>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor3<T0, T1, T2>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor4<T0, T1, T2, T3>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor4<T0, T1, T2, T3>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor5<T0, T1, T2, T3, T4>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor5<T0, T1, T2, T3, T4>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6, T7>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>?)predicate, ref job));
    }

    public static void SubmitPlainQuery<TJob, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IEcsScheduler scheduler, int queryId, object? predicate, in TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        SubmitPlainQueryCore(scheduler, queryId, predicate, in job, ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.GetPlainQueryExecutorId<TJob>(),
            static (World world, ArchQuery query, object? predicate, ref TJob job) => ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.ForEach(world, query, (ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>?)predicate, ref job));
    }

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

    private static void SubmitPlainQueryCore<TJob>(
        IEcsScheduler scheduler,
        int queryId,
        object? predicate,
        in TJob job,
        int executorId,
        PlainQueryExecute<TJob> execute)
        where TJob : struct
    {
        switch (scheduler)
        {
            case AsyncEcsScheduler asyncScheduler:
                SubmitAsync(asyncScheduler, queryId, predicate, in job, executorId, execute);
                return;
            case SyncEcsScheduler syncScheduler:
                SubmitSync(syncScheduler, queryId, predicate, in job, execute);
                return;
            default:
                throw new NotSupportedException($"Unsupported ECS scheduler type '{scheduler.GetType().FullName}'.");
        }
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

    private static void SubmitAsync<TJob>(
        AsyncEcsScheduler scheduler,
        int queryId,
        object? predicate,
        in TJob job,
        int executorId,
        PlainQueryExecute<TJob> execute)
        where TJob : struct
    {
        ArchQuery query = scheduler.Runtime.EcsQueryRegistry.Get(queryId);

        if (scheduler.IsSchedulerThread)
        {
            TJob localJob = job;
            execute(scheduler.World, query, predicate, ref localJob);
            return;
        }

        if (!RuntimeHelpers.IsReferenceOrContainsReferences<TJob>())
        {
            scheduler.RecordPlainQuery(executorId, query, predicate, in job);
            return;
        }

        var state = new PlainQueryFallback<TJob>(query, predicate, job, execute);
        scheduler.Schedule(PooledEcsWorkItem<PlainQueryFallback<TJob>>.Rent(
            "PlainQuery",
            in state,
            static (world, scheduledState) =>
            {
                TJob scheduledJob = scheduledState.Job;
                scheduledState.Execute(
                    world,
                    scheduledState.Query,
                    scheduledState.Predicate,
                    ref scheduledJob);
            }));
    }

    private static void SubmitSync<TJob>(
        SyncEcsScheduler scheduler,
        int queryId,
        object? predicate,
        in TJob job,
        PlainQueryExecute<TJob> execute)
        where TJob : struct
    {
        ArchQuery query = scheduler.Runtime.EcsQueryRegistry.Get(queryId);
        TJob localJob = job;
        execute(scheduler.World, query, predicate, ref localJob);
    }

    private readonly struct PlainQueryFallback<TJob>
        where TJob : struct
    {
        public PlainQueryFallback(
            ArchQuery query,
            object? predicate,
            TJob job,
            PlainQueryExecute<TJob> execute)
        {
            Query = query;
            Predicate = predicate;
            Job = job;
            Execute = execute;
        }

        public ArchQuery Query { get; }

        public object? Predicate { get; }

        public TJob Job { get; }

        public PlainQueryExecute<TJob> Execute { get; }
    }
}
