using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal static class ActorGeneratedAccess
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGeneratedActorMeta RequireGenerated(IActor actor)
    {
        if (actor == null)
        {
            throw new ArgumentNullException(nameof(actor));
        }

        if (actor is IGeneratedActorMeta generated)
        {
            return generated;
        }

        throw new InvalidOperationException(
            $"Actor type {actor.GetType().Name} does not provide generated actor metadata.");
    }
}