using LayerBase.Core.Event;

namespace LayerBase.Scope;

internal enum ScopeEventClass : byte
{
    Business = 0,
    Internal = 1,
    Critical = 2
}

internal readonly struct ScopeEventEnvelope
{
    public ScopeEventEnvelope(
        ScopeAddress origin,
        int routeId,
        ScopeEventClass @class,
        PayloadHandle payload)
    {
        Origin = origin;
        RouteId = routeId;
        Class = @class;
        Payload = payload;
    }

    public ScopeAddress Origin { get; }

    public int RouteId { get; }

    public ScopeEventClass Class { get; }

    public PayloadHandle Payload { get; }
}

internal static class ScopeEventClassExtensions
{
    public static ScopeAdmissionClass ToAdmissionClass(this ScopeEventClass @class)
    {
        return @class switch
        {
            ScopeEventClass.Business => ScopeAdmissionClass.Business,
            ScopeEventClass.Internal => ScopeAdmissionClass.Internal,
            ScopeEventClass.Critical => ScopeAdmissionClass.Critical,
            _ => ScopeAdmissionClass.Business
        };
    }
}

internal enum ScopeCallEnvelopeKind : byte
{
    Request = 0,
    Response = 1
}

internal enum ScopeCallClass : byte
{
    BusinessRequest = 0,
    Response = 1,
    Control = 2
}

internal enum ScopeCallTerminalState : byte
{
    None = 0,
    Succeeded = 1,
    Faulted = 2,
    Canceled = 3,
    ScopeStopped = 4
}

internal readonly struct ScopeCallToken : IEquatable<ScopeCallToken>
{
    public ScopeCallToken(int runtimeGeneration, int originScopeId, int sequence, int version)
    {
        RuntimeGeneration = runtimeGeneration;
        OriginScopeId = originScopeId;
        Sequence = sequence;
        Version = version;
    }

    public int RuntimeGeneration { get; }

    public int OriginScopeId { get; }

    public int Sequence { get; }

    public int Version { get; }

    public bool Equals(ScopeCallToken other)
    {
        return RuntimeGeneration == other.RuntimeGeneration &&
               OriginScopeId == other.OriginScopeId &&
               Sequence == other.Sequence &&
               Version == other.Version;
    }

    public override bool Equals(object? obj)
    {
        return obj is ScopeCallToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RuntimeGeneration, OriginScopeId, Sequence, Version);
    }

    public static bool operator ==(ScopeCallToken left, ScopeCallToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScopeCallToken left, ScopeCallToken right)
    {
        return !left.Equals(right);
    }
}

internal readonly struct ScopeCallResult
{
    public ScopeCallResult(ScopeCallTerminalState state)
    {
        State = state;
    }

    public ScopeCallTerminalState State { get; }

    public static ScopeCallResult None => new(ScopeCallTerminalState.None);

    public static ScopeCallResult Succeeded => new(ScopeCallTerminalState.Succeeded);

    public static ScopeCallResult Faulted => new(ScopeCallTerminalState.Faulted);

    public static ScopeCallResult Canceled => new(ScopeCallTerminalState.Canceled);

    public static ScopeCallResult ScopeStopped => new(ScopeCallTerminalState.ScopeStopped);
}

internal readonly struct ScopeCallEnvelope
{
    public ScopeCallEnvelope(
        ScopeCallEnvelopeKind kind,
        ScopeCallClass @class,
        ScopeCallToken token,
        ScopeAddress origin,
        int routeId,
        PayloadHandle payload,
        ScopeCallResult result,
        IScopeCallCompletion? completion = null)
    {
        Kind = kind;
        Class = @class;
        Token = token;
        Origin = origin;
        RouteId = routeId;
        Payload = payload;
        Result = result;
        Completion = completion;
    }

    public ScopeCallEnvelopeKind Kind { get; }

    public ScopeCallClass Class { get; }

    public ScopeCallToken Token { get; }

    public ScopeAddress Origin { get; }

    public int RouteId { get; }

    public PayloadHandle Payload { get; }

    public ScopeCallResult Result { get; }

    internal IScopeCallCompletion? Completion { get; }
}

internal static class ScopeCallClassExtensions
{
    public static ScopeAdmissionClass ToAdmissionClass(this ScopeCallClass @class)
    {
        return @class switch
        {
            ScopeCallClass.BusinessRequest => ScopeAdmissionClass.Business,
            ScopeCallClass.Response => ScopeAdmissionClass.Response,
            ScopeCallClass.Control => ScopeAdmissionClass.Control,
            _ => ScopeAdmissionClass.Business
        };
    }
}
