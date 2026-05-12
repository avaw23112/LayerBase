namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TagAttribute<TTag> : Attribute
    where TTag : struct, IActorTag
{
}