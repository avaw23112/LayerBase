using System;

namespace LayerBase.ECS.Projection
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ProjectedActorOptionsAttribute : Attribute
    {
        public ProjectedActorRetirePolicy RetirePolicy { get; }
        public ProjectedActorCreatePolicy CreatePolicy { get; }
        public float KeepAliveSeconds { get; }
        public float TouchIntervalSeconds { get; }

        public ProjectedActorOptionsAttribute(
            ProjectedActorRetirePolicy retirePolicy = ProjectedActorRetirePolicy.ReturnToPool,
            ProjectedActorCreatePolicy createPolicy = ProjectedActorCreatePolicy.Lazy,
            float keepAliveSeconds = 0.5f,
            float touchIntervalSeconds = 0.1f)
        {
            RetirePolicy = retirePolicy;
            CreatePolicy = createPolicy;
            KeepAliveSeconds = keepAliveSeconds;
            TouchIntervalSeconds = touchIntervalSeconds;
        }
    }
}

namespace LayerBase.Actor
{
    [Obsolete("Use ProjectedActorOptionsAttribute in LayerBase.ECS.Projection.")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ActorOptionsAttribute : Attribute
    {
        public LayerBase.ECS.Projection.ProjectedActorRetirePolicy RetirePolicy { get; }
        public LayerBase.ECS.Projection.ProjectedActorCreatePolicy CreatePolicy { get; }
        public float KeepAliveSeconds { get; }
        public float TouchIntervalSeconds { get; }

        public ActorOptionsAttribute(
            LayerBase.ECS.Projection.ProjectedActorRetirePolicy retirePolicy = LayerBase.ECS.Projection.ProjectedActorRetirePolicy.ReturnToPool,
            LayerBase.ECS.Projection.ProjectedActorCreatePolicy createPolicy = LayerBase.ECS.Projection.ProjectedActorCreatePolicy.Lazy,
            float keepAliveSeconds = 0.5f,
            float touchIntervalSeconds = 0.1f)
        {
            RetirePolicy = retirePolicy;
            CreatePolicy = createPolicy;
            KeepAliveSeconds = keepAliveSeconds;
            TouchIntervalSeconds = touchIntervalSeconds;
        }
    }
}
