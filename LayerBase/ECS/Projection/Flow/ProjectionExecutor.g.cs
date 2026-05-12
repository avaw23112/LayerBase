#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using LayerBase.Actor;
using LayerBase.ECS;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

internal static class ProjectionExecutor0
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate? predicate,
        ProjectionForEach<TEvent> forEach)
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
        ProjectionPredicate? predicate)
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
        ProjectionPredicate? predicate,
        ProjectionForEach<TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity))
            {
                continue;
            }

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
            TEvent output = default;
            forEach(in entity, ref output);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch.Add(actorId, in output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TouchChunk(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate? predicate,
        long nowTicks)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);
            if (predicate != null && !predicate(in entity))
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

internal static class ProjectionExecutor0_2E<TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach2<TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach2<TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor0_3E<TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach3<TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach3<TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor0_4E<TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor0_5E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor0_6E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor0_7E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor0_8E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor0_9E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor0_10E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate? predicate, ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
    {
        ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
        ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
        int count = chunk.Count;

        for (int row = 0; row < count; row++)
        {
            Entity entity = Unsafe.Add(ref firstEntity, row);

            if (predicate != null && !predicate(in entity))
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
        }
    }
}

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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob1x1<T0, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob1x1<T0, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor1_2E<T0, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach2<T0, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x2<T0, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob1x2<T0, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach2<T0, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor1_3E<T0, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x3<T0, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob1x3<T0, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor1_4E<T0, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x4<T0, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob1x4<T0, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor1_5E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob1x5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor1_6E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob1x6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor1_7E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob1x7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor1_8E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob1x8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor1_9E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob1x9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor1_10E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob1x10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob1x10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0>? predicate, ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob2x1<T0, T1, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob2x1<T0, T1, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor2_2E<T0, T1, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x2<T0, T1, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob2x2<T0, T1, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor2_3E<T0, T1, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x3<T0, T1, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob2x3<T0, T1, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor2_4E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob2x4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor2_5E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob2x5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor2_6E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob2x6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor2_7E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob2x7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor2_8E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob2x8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor2_9E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob2x9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor2_10E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob2x10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob2x10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate, ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor3_2E<T0, T1, T2, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x2<T0, T1, T2, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob3x2<T0, T1, T2, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor3_3E<T0, T1, T2, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x3<T0, T1, T2, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob3x3<T0, T1, T2, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor3_4E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob3x4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor3_5E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob3x5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor3_6E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob3x6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor3_7E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob3x7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor3_8E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob3x8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor3_9E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob3x9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor3_10E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob3x10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob3x10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2,
                ref c3);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor4_2E<T0, T1, T2, T3, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x2<T0, T1, T2, T3, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob4x2<T0, T1, T2, T3, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor4_3E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob4x3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor4_4E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob4x4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor4_5E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob4x5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor4_6E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob4x6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor4_7E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob4x7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor4_8E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob4x8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor4_9E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob4x9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor4_10E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob4x10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob4x10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2,
                ref c3,
                ref c4);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor5_2E<T0, T1, T2, T3, T4, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x2<T0, T1, T2, T3, T4, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob5x2<T0, T1, T2, T3, T4, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor5_3E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob5x3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor5_4E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob5x4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor5_5E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob5x5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor5_6E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob5x6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor5_7E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob5x7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor5_8E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob5x8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor5_9E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob5x9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor5_10E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob5x10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob5x10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2,
                ref c3,
                ref c4,
                ref c5);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor6_2E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob6x2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor6_3E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob6x3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor6_4E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob6x4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor6_5E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob6x5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor6_6E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob6x6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor6_7E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob6x7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor6_8E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob6x8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor6_9E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob6x9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor6_10E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob6x10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob6x10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2,
                ref c3,
                ref c4,
                ref c5,
                ref c6);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor7_2E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob7x2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor7_3E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob7x3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor7_4E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob7x4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor7_5E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob7x5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor7_6E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob7x6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor7_7E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob7x7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor7_8E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob7x8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor7_9E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob7x9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor7_10E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob7x10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob7x10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 = ref Unsafe.Add(ref first0, row);
            ref T1 c1 = ref Unsafe.Add(ref first1, row);
            ref T2 c2 = ref Unsafe.Add(ref first2, row);
            ref T3 c3 = ref Unsafe.Add(ref first3, row);
            ref T4 c4 = ref Unsafe.Add(ref first4, row);
            ref T5 c5 = ref Unsafe.Add(ref first5, row);
            ref T6 c6 = ref Unsafe.Add(ref first6, row);
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
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

    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            job.Execute(
                entity,
                ref c0,
                ref c1,
                ref c2,
                ref c3,
                ref c4,
                ref c5,
                ref c6,
                ref c7);
        }
    }

    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
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
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
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

internal static class ProjectionExecutor8_2E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TJob : struct, IProjectionJob8x2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
        }
    }
}

internal static class ProjectionExecutor8_3E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
        }
        finally
        {
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
        where TJob : struct, IProjectionJob8x3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
        }
    }
}

internal static class ProjectionExecutor8_4E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
        }
        finally
        {
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
        where TJob : struct, IProjectionJob8x4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
        }
    }
}

internal static class ProjectionExecutor8_5E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
        }
        finally
        {
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
        where TJob : struct, IProjectionJob8x5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
        }
    }
}

internal static class ProjectionExecutor8_6E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
        }
        finally
        {
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
        where TJob : struct, IProjectionJob8x6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
        }
    }
}

internal static class ProjectionExecutor8_7E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
        }
        finally
        {
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
        where TJob : struct, IProjectionJob8x7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
        }
    }
}

internal static class ProjectionExecutor8_8E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
        }
        finally
        {
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
        where TJob : struct, IProjectionJob8x8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
        }
    }
}

internal static class ProjectionExecutor8_9E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
        }
        finally
        {
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
        where TJob : struct, IProjectionJob8x9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
        }
    }
}

internal static class ProjectionExecutor8_10E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    public static void Post(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        ActorWorld actorWorld = world.Runtime.Actors;
        long nowTicks = Stopwatch.GetTimestamp();
        ProjectionBatchBuffer<TEvent0> batch0 = ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 = ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 = ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 = ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 = ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 = ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 = ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 = ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 = ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 = ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(world, actorWorld, ref chunk, predicate, forEach, nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(actorWorld);
            batch1.PostTo(actorWorld);
            batch2.PostTo(actorWorld);
            batch3.PostTo(actorWorld);
            batch4.PostTo(actorWorld);
            batch5.PostTo(actorWorld);
            batch6.PostTo(actorWorld);
            batch7.PostTo(actorWorld);
            batch8.PostTo(actorWorld);
            batch9.PostTo(actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob8x10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();
        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();
        ProjectionBatchBuffer<TEvent2> batch2 =
            ProjectionBatchBuffer<TEvent2>.Rent();
        ProjectionBatchBuffer<TEvent3> batch3 =
            ProjectionBatchBuffer<TEvent3>.Rent();
        ProjectionBatchBuffer<TEvent4> batch4 =
            ProjectionBatchBuffer<TEvent4>.Rent();
        ProjectionBatchBuffer<TEvent5> batch5 =
            ProjectionBatchBuffer<TEvent5>.Rent();
        ProjectionBatchBuffer<TEvent6> batch6 =
            ProjectionBatchBuffer<TEvent6>.Rent();
        ProjectionBatchBuffer<TEvent7> batch7 =
            ProjectionBatchBuffer<TEvent7>.Rent();
        ProjectionBatchBuffer<TEvent8> batch8 =
            ProjectionBatchBuffer<TEvent8>.Rent();
        ProjectionBatchBuffer<TEvent9> batch9 =
            ProjectionBatchBuffer<TEvent9>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0,
                    ref batch1,
                    ref batch2,
                    ref batch3,
                    ref batch4,
                    ref batch5,
                    ref batch6,
                    ref batch7,
                    ref batch8,
                    ref batch9);
            }

            batch0.PostTo(
                actorWorld);
            batch1.PostTo(
                actorWorld);
            batch2.PostTo(
                actorWorld);
            batch3.PostTo(
                actorWorld);
            batch4.PostTo(
                actorWorld);
            batch5.PostTo(
                actorWorld);
            batch6.PostTo(
                actorWorld);
            batch7.PostTo(
                actorWorld);
            batch8.PostTo(
                actorWorld);
            batch9.PostTo(
                actorWorld);
        }
        finally
        {
            batch9.Dispose();
            batch8.Dispose();
            batch7.Dispose();
            batch6.Dispose();
            batch5.Dispose();
            batch4.Dispose();
            batch3.Dispose();
            batch2.Dispose();
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
        where TJob : struct, IProjectionJob8x10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        ref T0 first0 = ref chunk.GetFirst<T0>();
        ref T1 first1 = ref chunk.GetFirst<T1>();
        ref T2 first2 = ref chunk.GetFirst<T2>();
        ref T3 first3 = ref chunk.GetFirst<T3>();
        ref T4 first4 = ref chunk.GetFirst<T4>();
        ref T5 first5 = ref chunk.GetFirst<T5>();
        ref T6 first6 = ref chunk.GetFirst<T6>();
        ref T7 first7 = ref chunk.GetFirst<T7>();
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

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
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(in entity, in c0, in c1, in c2, in c3, in c4, in c5, in c6, in c7))
            {
                continue;
            }

            TEvent0 e0 =
                default;
            TEvent1 e1 =
                default;
            TEvent2 e2 =
                default;
            TEvent3 e3 =
                default;
            TEvent4 e4 =
                default;
            TEvent5 e5 =
                default;
            TEvent6 e6 =
                default;
            TEvent7 e7 =
                default;
            TEvent8 e8 =
                default;
            TEvent9 e9 =
                default;

            ProjectResult result =
                job.Execute(
                    entity,
                    ref c0,
                    ref c1,
                    ref c2,
                    ref c3,
                    ref c4,
                    ref c5,
                    ref c6,
                    ref c7,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3,
                    ref e4,
                    ref e5,
                    ref e6,
                    ref e7,
                    ref e8,
                    ref e9);

            if (result == ProjectResult.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            if (result == ProjectResult.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
            batch1.Add(
                actorId,
                in e1);
            batch2.Add(
                actorId,
                in e2);
            batch3.Add(
                actorId,
                in e3);
            batch4.Add(
                actorId,
                in e4);
            batch5.Add(
                actorId,
                in e5);
            batch6.Add(
                actorId,
                in e6);
            batch7.Add(
                actorId,
                in e7);
            batch8.Add(
                actorId,
                in e8);
            batch9.Add(
                actorId,
                in e9);
        }
    }

    private static void CollectPostChunk(World world, ActorWorld actorWorld, ref Chunk chunk,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach, long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1,
        ref ProjectionBatchBuffer<TEvent2> batch2,
        ref ProjectionBatchBuffer<TEvent3> batch3,
        ref ProjectionBatchBuffer<TEvent4> batch4,
        ref ProjectionBatchBuffer<TEvent5> batch5,
        ref ProjectionBatchBuffer<TEvent6> batch6,
        ref ProjectionBatchBuffer<TEvent7> batch7,
        ref ProjectionBatchBuffer<TEvent8> batch8,
        ref ProjectionBatchBuffer<TEvent9> batch9)
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
                continue;

            ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

            TEvent0 e0 = default;
            TEvent1 e1 = default;
            TEvent2 e2 = default;
            TEvent3 e3 = default;
            TEvent4 e4 = default;
            TEvent5 e5 = default;
            TEvent6 e6 = default;
            TEvent7 e7 = default;
            TEvent8 e8 = default;
            TEvent9 e9 = default;

            forEach(in entity, ref c0, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref e0, ref e1, ref e2, ref e3, ref e4, ref e5, ref e6, ref e7, ref e8, ref e9);

            ActorId actorId = meta.ActorId;
            if (!actorId.IsValid)
            {
                actorId = ProjectedActorBinding.EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks);
                if (!actorId.IsValid) continue;
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(actorWorld, ref meta, nowTicks);
                actorId = meta.ActorId;
                if (!actorId.IsValid) continue;
            }

            batch0.Add(actorId, in e0);
            batch1.Add(actorId, in e1);
            batch2.Add(actorId, in e2);
            batch3.Add(actorId, in e3);
            batch4.Add(actorId, in e4);
            batch5.Add(actorId, in e5);
            batch6.Add(actorId, in e6);
            batch7.Add(actorId, in e7);
            batch8.Add(actorId, in e8);
            batch9.Add(actorId, in e9);
        }
    }
}

