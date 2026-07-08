namespace LayerBase.ECS.Runtime.Query;

public readonly struct EcsQueryKey : IEquatable<EcsQueryKey>
{
    private readonly RuntimeTypeHandle _t0;
    private readonly RuntimeTypeHandle _t1;
    private readonly RuntimeTypeHandle _t2;
    private readonly RuntimeTypeHandle _t3;
    private readonly RuntimeTypeHandle _t4;
    private readonly RuntimeTypeHandle _t5;
    private readonly RuntimeTypeHandle _t6;
    private readonly RuntimeTypeHandle _t7;
    private readonly RuntimeTypeHandle _t8;
    private readonly RuntimeTypeHandle _t9;
    private readonly RuntimeTypeHandle _t10;
    private readonly RuntimeTypeHandle _t11;
    private readonly byte _arity;

    private EcsQueryKey(
        byte arity,
        RuntimeTypeHandle t0 = default,
        RuntimeTypeHandle t1 = default,
        RuntimeTypeHandle t2 = default,
        RuntimeTypeHandle t3 = default,
        RuntimeTypeHandle t4 = default,
        RuntimeTypeHandle t5 = default,
        RuntimeTypeHandle t6 = default,
        RuntimeTypeHandle t7 = default,
        RuntimeTypeHandle t8 = default,
        RuntimeTypeHandle t9 = default,
        RuntimeTypeHandle t10 = default,
        RuntimeTypeHandle t11 = default)
    {
        _arity = arity;
        _t0 = t0;
        _t1 = t1;
        _t2 = t2;
        _t3 = t3;
        _t4 = t4;
        _t5 = t5;
        _t6 = t6;
        _t7 = t7;
        _t8 = t8;
        _t9 = t9;
        _t10 = t10;
        _t11 = t11;
    }

    public static EcsQueryKey Create()
    {
        return new EcsQueryKey(0);
    }

    public static EcsQueryKey Create<T0>()
    {
        return new EcsQueryKey(1, typeof(T0).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1>()
    {
        return new EcsQueryKey(2, typeof(T0).TypeHandle, typeof(T1).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2>()
    {
        return new EcsQueryKey(3, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3>()
    {
        return new EcsQueryKey(4, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4>()
    {
        return new EcsQueryKey(5, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5>()
    {
        return new EcsQueryKey(6, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6>()
    {
        return new EcsQueryKey(7, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6, T7>()
    {
        return new EcsQueryKey(8, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle, typeof(T7).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6, T7, T8>()
    {
        return new EcsQueryKey(9, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle, typeof(T7).TypeHandle, typeof(T8).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>()
    {
        return new EcsQueryKey(10, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle, typeof(T7).TypeHandle, typeof(T8).TypeHandle, typeof(T9).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
    {
        return new EcsQueryKey(11, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle, typeof(T7).TypeHandle, typeof(T8).TypeHandle, typeof(T9).TypeHandle, typeof(T10).TypeHandle);
    }

    public static EcsQueryKey Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
    {
        return new EcsQueryKey(12, typeof(T0).TypeHandle, typeof(T1).TypeHandle, typeof(T2).TypeHandle, typeof(T3).TypeHandle, typeof(T4).TypeHandle, typeof(T5).TypeHandle, typeof(T6).TypeHandle, typeof(T7).TypeHandle, typeof(T8).TypeHandle, typeof(T9).TypeHandle, typeof(T10).TypeHandle, typeof(T11).TypeHandle);
    }

    public bool Equals(EcsQueryKey other)
    {
        return _arity == other._arity &&
               _t0.Equals(other._t0) &&
               _t1.Equals(other._t1) &&
               _t2.Equals(other._t2) &&
               _t3.Equals(other._t3) &&
               _t4.Equals(other._t4) &&
               _t5.Equals(other._t5) &&
               _t6.Equals(other._t6) &&
               _t7.Equals(other._t7) &&
               _t8.Equals(other._t8) &&
               _t9.Equals(other._t9) &&
               _t10.Equals(other._t10) &&
               _t11.Equals(other._t11);
    }

    public override bool Equals(object? obj)
    {
        return obj is EcsQueryKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(_arity);
        hash.Add(_t0);
        hash.Add(_t1);
        hash.Add(_t2);
        hash.Add(_t3);
        hash.Add(_t4);
        hash.Add(_t5);
        hash.Add(_t6);
        hash.Add(_t7);
        hash.Add(_t8);
        hash.Add(_t9);
        hash.Add(_t10);
        hash.Add(_t11);
        return hash.ToHashCode();
    }
}
