#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using LayerBase.Actor;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

internal static class ProjectionExecutor1<T0>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ProjectionForEach<T0, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ProjectionForEach<T0, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor2<T0, T1>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach<T0, T1, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach<T0, T1, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor3<T0, T1, T2>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ProjectionForEach<T0, T1, T2, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ProjectionForEach<T0, T1, T2, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor4<T0, T1, T2, T3>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ProjectionForEach<T0, T1, T2, T3, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ProjectionForEach<T0, T1, T2, T3, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor5<T0, T1, T2, T3, T4>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor6<T0, T1, T2, T3, T4, T5>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}

internal static class ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent> batch = ProjectionBatchBuffer<TEvent>.Rent();
        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks, ref batch);
            }

            batch.PostTo(actorWorld);
        }
        finally
        {
            batch.Dispose();
        }
    }

    public static void Touch(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            TouchChunk(world, actorWorld, ref chunk, predicate, nowTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            ref T7 c7 = ref Unsafe.Add(ref first7, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        long nowTicks)
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            ref T7 c7 = ref Unsafe.Add(ref first7, row);
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            if (!meta.ActorId.IsValid)
            {
                _ = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
            }
        }
    }
}
