using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase;

public partial class LayerRuntime
{
    public PostResult Post<TEvent>(
        ActorId              actorId,
        in TEvent            value,
        ActorPostPolicy?     postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return Actors.Post(actorId,in value, postPolicy, fullPolicy);
    }

    public PostResult TryPost<TEvent>(
        ActorId              actorId,
        in TEvent            value,
        ActorPostPolicy?     postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return Actors.TryPost(actorId,in value, postPolicy, fullPolicy);
    }

    public void PostMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value,
        ActorPostPolicy?      postPolicy = null,
        ActorMailFullPolicy?  fullPolicy = null)
        where TEvent : struct
    {
         Actors.PostMany(actorIds,in value, postPolicy, fullPolicy);
    }
}