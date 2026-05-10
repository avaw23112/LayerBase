namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehaviourAttribute : Attribute
{
    public ActorBehaviourAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehavioursAttribute : Attribute
{
    public ActorBehavioursAttribute()
    {
    }
}
