#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public delegate bool ProjectionPredicate<T0>(
        in Entity entity,
        in T0 c0);

public delegate void ProjectionForEach<T0, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1>(
        in Entity entity,
        in T0 c0,
        in T1 c1);

public delegate void ProjectionForEach<T0, T1, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2);

public delegate void ProjectionForEach<T0, T1, T2, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3);

public delegate void ProjectionForEach<T0, T1, T2, T3, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4);

public delegate void ProjectionForEach<T0, T1, T2, T3, T4, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5);

public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6);

public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent output)
        where TEvent : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6,
        in T7 c7);

public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent output)
        where TEvent : struct;
