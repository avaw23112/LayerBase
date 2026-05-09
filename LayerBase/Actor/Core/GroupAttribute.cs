namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class GroupAttribute<TGroup> : Attribute
    where TGroup : struct, IActorGroup
{
}
