#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public delegate bool ProjectionPredicate(
        in Entity entity);
public delegate void ProjectionForEach<TEvent0>(
        in Entity entity,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<TEvent0, TEvent1>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0>(
        in Entity entity,
        in T0 c0);
public delegate void ProjectionForEach<T0, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1>(
        in Entity entity,
        in T0 c0,
        in T1 c1);
public delegate void ProjectionForEach<T0, T1, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2);
public delegate void ProjectionForEach<T0, T1, T2, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3);
public delegate void ProjectionForEach<T0, T1, T2, T3, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

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
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct;

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct;

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct;

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct;

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct;

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct;

