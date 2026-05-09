namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehaviourAttribute : Attribute
{
    public ActorBehaviourAttribute(BehaviourType behaviourType = BehaviourType.Cold)
    {
        BehaviourType = behaviourType;
    }

    public BehaviourType BehaviourType { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehavioursAttribute : Attribute
{
    public ActorBehavioursAttribute(BehaviourType behaviourType = BehaviourType.Cold)
    {
        BehaviourType = behaviourType;
    }

    public BehaviourType BehaviourType { get; }
}
