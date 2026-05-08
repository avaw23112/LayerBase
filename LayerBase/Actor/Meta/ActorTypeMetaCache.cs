namespace LayerBase.Actor;

internal static class ActorTypeMetaCache<TActor>
    where TActor : class, IActor
{
    public static ActorTypeMeta<TActor>? Value;
}

internal static class ActorTypeMetaCache
{
    public static ActorTypeMeta<TActor> GetOrBuild<TActor>(IGeneratedActorMeta generated)
        where TActor : class, IActor
    {
        if (generated == null)
        {
            throw new ArgumentNullException(nameof(generated));
        }

        ActorTypeMeta<TActor>? cached = ActorTypeMetaCache<TActor>.Value;
        if (cached != null)
        {
            return cached;
        }

        var builder = new ActorTypeMetaBuilder();
        generated.__BuildActorMeta(builder);

        ActorTypeMeta<TActor> meta = builder.Build<TActor>();
        ActorTypeMetaCache<TActor>.Value = meta;
        return meta;
    }
}
