using Arch.Core;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Runtime.Query;

public static class EcsQueryDescriptionCache
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
        return description;
    }
}

public static class EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
{
    public static readonly QueryDescription Description = Build();

    private static QueryDescription Build()
    {
        QueryDescription description = new QueryDescription();
        description.WithAll<ProjectedActorRef>();
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
        return description;
    }
}
