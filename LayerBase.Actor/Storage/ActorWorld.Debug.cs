using System.Text;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public ActorDebugInfo GetDebugInfo(ActorId actorId)
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return ActorDebugInfo.Invalid(actorId, "Invalid ArchetypeId.");
        }

        return _archetypes[actorId.ArchetypeId].GetDebugInfo(actorId);
    }

    public string DescribeActor(ActorId actorId)
    {
        ActorDebugInfo info = GetDebugInfo(actorId);
        var builder = new StringBuilder();

        builder.AppendLine("Actor Debug Info");
        builder.AppendLine("----------------");
        builder.AppendLine($"ActorId: Archetype={actorId.ArchetypeId}, Slot={actorId.SlotIndex}, Generation={actorId.Generation}");
        builder.AppendLine($"Valid: {info.IsValid}");
        builder.AppendLine($"Alive: {info.IsAlive}");
        builder.AppendLine($"Enabled: {info.IsEnabled}");
        builder.AppendLine($"PendingDestroy: {info.IsPendingDestroy}");
        builder.AppendLine($"Type: {info.ActorTypeName}");
        builder.AppendLine($"Archetype: {info.ArchetypeInfo}");
        builder.AppendLine($"Tags: {string.Join(", ", info.Tags)}");
        builder.AppendLine($"Groups: {string.Join(", ", info.Groups)}");
        builder.AppendLine($"PendingMailCount: {info.PendingMailCount}");
        builder.AppendLine($"Lifecycle: Update={info.HasUpdate}, LateUpdate={info.HasLateUpdate}, FixedUpdate={info.HasFixedUpdate}");

        if (!string.IsNullOrEmpty(info.FailureReason))
        {
            builder.AppendLine($"Failure: {info.FailureReason}");
        }

        return builder.ToString();
    }

    public string DumpActorWorld()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# ActorWorld Dump");
        builder.AppendLine();
        builder.AppendLine("| ArchetypeId | Archetype | ActorType | Alive | Enabled | PendingDestroy | MailCount |");
        builder.AppendLine("| -- | -- | -- | -- | -- | -- | -- |");

        foreach (BehaviourArchetype archetype in _archetypes)
        {
            archetype.AppendDebugRows(builder);
        }

        return builder.ToString();
    }

    public string DumpQuery(ActorQueryResult query)
    {
        query = query.RefreshIfNeeded();

        var builder = new StringBuilder();
        builder.AppendLine("# ActorQuery Dump");
        builder.AppendLine($"QueryVersion: {QueryVersion}");
        builder.AppendLine($"MatchedArchetypes: {query.Cache.Archetypes.Length}");
        builder.AppendLine($"AliveCount: {query.CountAlive()}");
        builder.AppendLine($"EnabledCount: {query.CountEnabled()}");

        for (int i = 0; i < query.Cache.Archetypes.Length; i++)
        {
            BehaviourArchetype archetype = query.Cache.Archetypes[i];
            builder.Append("- ");
            builder.Append(archetype.ArchetypeId);
            builder.Append(": ");
            builder.Append(archetype.Describe());
            builder.Append(" | Alive=");
            builder.Append(archetype.CountAlive());
            builder.Append(" | Enabled=");
            builder.Append(archetype.CountEnabled());
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
