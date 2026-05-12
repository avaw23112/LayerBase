namespace LayerBase.Actor;

public readonly struct ActorDebugInfo
{
    public readonly ActorId ActorId;
    public readonly bool IsValid;
    public readonly bool IsAlive;
    public readonly bool IsEnabled;
    public readonly bool IsPendingDestroy;
    public readonly string ActorTypeName;
    public readonly string ArchetypeInfo;
    public readonly string[] Tags;
    public readonly string[] Groups;
    public readonly int PendingMailCount;
    public readonly bool HasUpdate;
    public readonly bool HasLateUpdate;
    public readonly bool HasFixedUpdate;
    public readonly string FailureReason;

    public ActorDebugInfo(
        ActorId  actorId,
        bool     isValid,
        bool     isAlive,
        bool     isEnabled,
        bool     isPendingDestroy,
        string   actorTypeName,
        string   archetypeInfo,
        string[] tags,
        string[] groups,
        int      pendingMailCount,
        bool     hasUpdate,
        bool     hasLateUpdate,
        bool     hasFixedUpdate,
        string   failureReason)
    {
        ActorId = actorId;
        IsValid = isValid;
        IsAlive = isAlive;
        IsEnabled = isEnabled;
        IsPendingDestroy = isPendingDestroy;
        ActorTypeName = actorTypeName ?? string.Empty;
        ArchetypeInfo = archetypeInfo ?? string.Empty;
        Tags = tags ?? Array.Empty<string>();
        Groups = groups ?? Array.Empty<string>();
        PendingMailCount = pendingMailCount;
        HasUpdate = hasUpdate;
        HasLateUpdate = hasLateUpdate;
        HasFixedUpdate = hasFixedUpdate;
        FailureReason = failureReason ?? string.Empty;
    }

    public static ActorDebugInfo Invalid(ActorId actorId, string reason)
    {
        return new ActorDebugInfo(
            actorId,
            isValid: false,
            isAlive: false,
            isEnabled: false,
            isPendingDestroy: false,
            actorTypeName: string.Empty,
            archetypeInfo: string.Empty,
            tags: Array.Empty<string>(),
            groups: Array.Empty<string>(),
            pendingMailCount: 0,
            hasUpdate: false,
            hasLateUpdate: false,
            hasFixedUpdate: false,
            failureReason: reason);
    }
}