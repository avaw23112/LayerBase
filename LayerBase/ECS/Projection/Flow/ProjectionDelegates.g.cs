#nullable enable
using Arch.Core;
using LayerBase.ECS;

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

public delegate void ProjectionForEach11<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6,
        in T7 c7,
        in T8 c8);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6,
        in T7 c7,
        in T8 c8,
        in T9 c9);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6,
        in T7 c7,
        in T8 c8,
        in T9 c9,
        in T10 c10);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

public delegate bool ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        in Entity entity,
        in T0 c0,
        in T1 c1,
        in T2 c2,
        in T3 c3,
        in T4 c4,
        in T5 c5,
        in T6 c6,
        in T7 c7,
        in T8 c8,
        in T9 c9,
        in T10 c10,
        in T11 c11);
public delegate void ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;

public delegate void ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct;

public delegate void ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
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

public delegate void ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10)
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
    where TEvent10 : struct;

public delegate void ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        in Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11)
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
    where TEvent10 : struct
    where TEvent11 : struct;

public interface IQueryJob<T0>
{
    void Execute(
        Entity entity,
        ref T0 c0);
}

public interface IQueryJob<T0, T1>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1);
}

public interface IQueryJob<T0, T1, T2>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2);
}

public interface IQueryJob<T0, T1, T2, T3>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3);
}

public interface IQueryJob<T0, T1, T2, T3, T4>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10);
}

public interface IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
{
    void Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11);
}

public interface IProjectionJob1x1<T0, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0);
}

public interface IProjectionJob1x2<T0, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob1x3<T0, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob1x4<T0, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob1x5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob1x6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob1x7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob1x8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob1x9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8);
}

public interface IProjectionJob1x10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob1x11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob1x12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob2x1<T0, T1, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0);
}

public interface IProjectionJob2x2<T0, T1, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob2x3<T0, T1, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob2x4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob2x5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob2x6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob2x7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob2x8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob2x9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob2x10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob2x11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob2x12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob3x1<T0, T1, T2, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0);
}

public interface IProjectionJob3x2<T0, T1, T2, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob3x3<T0, T1, T2, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob3x4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob3x5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob3x6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob3x7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob3x8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob3x9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob3x10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob3x11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob3x12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0);
}

public interface IProjectionJob4x2<T0, T1, T2, T3, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob4x3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob4x4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob4x5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob4x6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob4x7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent6 e6);
}

public interface IProjectionJob4x8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob4x9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob4x10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob4x11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob4x12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0);
}

public interface IProjectionJob5x2<T0, T1, T2, T3, T4, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob5x3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob5x4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob5x5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob5x6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent5 e5);
}

public interface IProjectionJob5x7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent6 e6);
}

public interface IProjectionJob5x8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob5x9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob5x10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob5x11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob5x12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0);
}

public interface IProjectionJob6x2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob6x3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob6x4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob6x5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent4 e4);
}

public interface IProjectionJob6x6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent5 e5);
}

public interface IProjectionJob6x7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent6 e6);
}

public interface IProjectionJob6x8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob6x9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob6x10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob6x11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob6x12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0);
}

public interface IProjectionJob7x2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob7x3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob7x4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent3 e3);
}

public interface IProjectionJob7x5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent4 e4);
}

public interface IProjectionJob7x6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent5 e5);
}

public interface IProjectionJob7x7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent6 e6);
}

public interface IProjectionJob7x8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob7x9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob7x10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob7x11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob7x12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0);
}

public interface IProjectionJob8x2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob8x3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent2 e2);
}

public interface IProjectionJob8x4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent3 e3);
}

public interface IProjectionJob8x5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent4 e4);
}

public interface IProjectionJob8x6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent5 e5);
}

public interface IProjectionJob8x7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent6 e6);
}

public interface IProjectionJob8x8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent7 e7);
}

public interface IProjectionJob8x9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent8 e8);
}

public interface IProjectionJob8x10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9);
}

public interface IProjectionJob8x11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob8x12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
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
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob9x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0);
}

public interface IProjectionJob9x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob9x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob9x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob9x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob9x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob9x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob9x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob9x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8);
}

public interface IProjectionJob9x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9);
}

public interface IProjectionJob9x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob9x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob10x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0);
}

public interface IProjectionJob10x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob10x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob10x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob10x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob10x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob10x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob10x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob10x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8);
}

public interface IProjectionJob10x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9);
}

public interface IProjectionJob10x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob10x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob11x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0);
}

public interface IProjectionJob11x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob11x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob11x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob11x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob11x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob11x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob11x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob11x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8);
}

public interface IProjectionJob11x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9);
}

public interface IProjectionJob11x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob11x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

public interface IProjectionJob12x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0);
}

public interface IProjectionJob12x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1);
}

public interface IProjectionJob12x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2);
}

public interface IProjectionJob12x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3);
}

public interface IProjectionJob12x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4);
}

public interface IProjectionJob12x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5);
}

public interface IProjectionJob12x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6);
}

public interface IProjectionJob12x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7);
}

public interface IProjectionJob12x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8);
}

public interface IProjectionJob12x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9);
}

public interface IProjectionJob12x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10);
}

public interface IProjectionJob12x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
{
    ProjectResult Execute(
        Entity entity,
        ref T0 c0,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8,
        ref T9 c9,
        ref T10 c10,
        ref T11 c11,
        ref TEvent0 e0,
        ref TEvent1 e1,
        ref TEvent2 e2,
        ref TEvent3 e3,
        ref TEvent4 e4,
        ref TEvent5 e5,
        ref TEvent6 e6,
        ref TEvent7 e7,
        ref TEvent8 e8,
        ref TEvent9 e9,
        ref TEvent10 e10,
        ref TEvent11 e11);
}

