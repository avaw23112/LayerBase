using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Snap;

namespace LayerBase.Scope;

internal enum ScopeLifecyclePhase
{
    Initialize,
    PostBuild,
    RuntimeStart,
    RuntimeStop,
    Dispose
}

internal enum ScopeControlResult : byte
{
    Succeeded = 0,
    Faulted = 1,
    Rejected = 2
}

internal readonly struct ScopeStopCall
{
}

internal readonly struct ScopeStopResponse
{
    public ScopeStopResponse(ScopeControlResult state)
    {
        State = state;
    }

    public ScopeControlResult State { get; }
}

internal readonly struct ScopeDisposeCall
{
}

internal readonly struct ScopeDisposeResponse
{
    public ScopeDisposeResponse(ScopeControlResult state)
    {
        State = state;
    }

    public ScopeControlResult State { get; }
}

internal readonly struct ScopePrewarmCall
{
    public ScopePrewarmCall(LayerPrewarmOptions options)
    {
        Options = options;
    }

    public LayerPrewarmOptions Options { get; }
}

internal readonly struct ScopePrewarmResponse
{
    public ScopePrewarmResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeFreezeRuntimeRegistriesCall
{
}

internal readonly struct ScopeFreezeRuntimeRegistriesResponse
{
    public ScopeFreezeRuntimeRegistriesResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeRecompileTimerPlansCall
{
}

internal readonly struct ScopeRecompileTimerPlansResponse
{
    public ScopeRecompileTimerPlansResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeEnterSafePointCall
{
}

internal readonly struct ScopeEnterSafePointResponse
{
    public ScopeEnterSafePointResponse(ScopeControlResult result, long token)
    {
        Result = result;
        Token = token;
    }

    public ScopeControlResult Result { get; }

    public long Token { get; }
}

internal readonly struct ScopeWriteSnapshotCall
{
}

internal readonly struct ScopeWriteSnapshotResponse
{
    public ScopeWriteSnapshotResponse(ScopeControlResult result, SnapSection[] sections)
    {
        Result = result;
        Sections = sections ?? Array.Empty<SnapSection>();
    }

    public ScopeControlResult Result { get; }

    public SnapSection[] Sections { get; }
}

internal readonly struct ScopeReadSnapshotCall
{
    public ScopeReadSnapshotCall(SnapDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public SnapDocument Document { get; }
}

internal readonly struct ScopeReadSnapshotResponse
{
    public ScopeReadSnapshotResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeExitSafePointCall
{
}

internal readonly struct ScopeExitSafePointResponse
{
    public ScopeExitSafePointResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeCaptureDiagnosticsCall
{
}

internal readonly struct ScopeInitializeCall
{
}

internal readonly struct ScopeInitializeResponse
{
    public ScopeInitializeResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopePostBuildCall
{
}

internal readonly struct ScopePostBuildResponse
{
    public ScopePostBuildResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeRuntimeStartCall
{
}

internal readonly struct ScopeRuntimeStartResponse
{
    public ScopeRuntimeStartResponse(ScopeControlResult result)
    {
        Result = result;
    }

    public ScopeControlResult Result { get; }
}

internal readonly struct ScopeCaptureDiagnosticsResponse
{
    public ScopeCaptureDiagnosticsResponse(
        ScopeControlResult result,
        ScopeDiagnosticsSnapshot snapshot)
    {
        Result = result;
        Snapshot = snapshot;
    }

    public ScopeControlResult Result { get; }

    public ScopeDiagnosticsSnapshot Snapshot { get; }
}

internal static class ScopeLifecycleRouteIds
{
    public const int Stop = -101;

    public const int Dispose = -102;

    public const int EnterSafePoint = -103;

    public const int WriteSnapshot = -104;

    public const int ReadSnapshot = -105;

    public const int ExitSafePoint = -106;

    public const int CaptureDiagnostics = -107;

    public const int Initialize = -108;

    public const int PostBuild = -109;

    public const int RuntimeStart = -110;

    public const int Prewarm = -111;

    public const int FreezeRuntimeRegistries = -112;

    public const int RecompileTimerPlans = -113;

    public static int Resolve<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        if (typeof(TRequest) == typeof(ScopeStopCall) &&
            typeof(TResponse) == typeof(ScopeStopResponse))
        {
            return Stop;
        }

        if (typeof(TRequest) == typeof(ScopeDisposeCall) &&
            typeof(TResponse) == typeof(ScopeDisposeResponse))
        {
            return Dispose;
        }

        if (typeof(TRequest) == typeof(ScopeEnterSafePointCall) &&
            typeof(TResponse) == typeof(ScopeEnterSafePointResponse))
        {
            return EnterSafePoint;
        }

        if (typeof(TRequest) == typeof(ScopeWriteSnapshotCall) &&
            typeof(TResponse) == typeof(ScopeWriteSnapshotResponse))
        {
            return WriteSnapshot;
        }

        if (typeof(TRequest) == typeof(ScopeReadSnapshotCall) &&
            typeof(TResponse) == typeof(ScopeReadSnapshotResponse))
        {
            return ReadSnapshot;
        }

        if (typeof(TRequest) == typeof(ScopeExitSafePointCall) &&
            typeof(TResponse) == typeof(ScopeExitSafePointResponse))
        {
            return ExitSafePoint;
        }

        if (typeof(TRequest) == typeof(ScopeCaptureDiagnosticsCall) &&
            typeof(TResponse) == typeof(ScopeCaptureDiagnosticsResponse))
        {
            return CaptureDiagnostics;
        }

        if (typeof(TRequest) == typeof(ScopeInitializeCall) &&
            typeof(TResponse) == typeof(ScopeInitializeResponse))
        {
            return Initialize;
        }

        if (typeof(TRequest) == typeof(ScopePostBuildCall) &&
            typeof(TResponse) == typeof(ScopePostBuildResponse))
        {
            return PostBuild;
        }

        if (typeof(TRequest) == typeof(ScopeRuntimeStartCall) &&
            typeof(TResponse) == typeof(ScopeRuntimeStartResponse))
        {
            return RuntimeStart;
        }

        if (typeof(TRequest) == typeof(ScopePrewarmCall) &&
            typeof(TResponse) == typeof(ScopePrewarmResponse))
        {
            return Prewarm;
        }

        if (typeof(TRequest) == typeof(ScopeFreezeRuntimeRegistriesCall) &&
            typeof(TResponse) == typeof(ScopeFreezeRuntimeRegistriesResponse))
        {
            return FreezeRuntimeRegistries;
        }

        if (typeof(TRequest) == typeof(ScopeRecompileTimerPlansCall) &&
            typeof(TResponse) == typeof(ScopeRecompileTimerPlansResponse))
        {
            return RecompileTimerPlans;
        }

        throw new InvalidOperationException(
            $"Unsupported scope lifecycle control call {typeof(TRequest).Name}/{typeof(TResponse).Name}.");
    }
}

internal static class ScopeLifecycleControlExtensions
{
    public static LBTask<ScopeStopResponse> RequestStopAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeStopCall, ScopeStopResponse>(new ScopeStopCall());
    }

    public static LBTask<ScopeDisposeResponse> RequestDisposeAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeDisposeCall, ScopeDisposeResponse>(new ScopeDisposeCall());
    }

    public static LBTask<ScopeEnterSafePointResponse> RequestEnterSafePointAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeEnterSafePointCall, ScopeEnterSafePointResponse>(
            new ScopeEnterSafePointCall(),
            cancellationToken);
    }

    public static LBTask<ScopeWriteSnapshotResponse> RequestWriteSnapshotAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeWriteSnapshotCall, ScopeWriteSnapshotResponse>(
            new ScopeWriteSnapshotCall(),
            cancellationToken);
    }

    public static LBTask<ScopeReadSnapshotResponse> RequestReadSnapshotAsync(
        this ScopeRuntime scope,
        SnapDocument document,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeReadSnapshotCall, ScopeReadSnapshotResponse>(
            new ScopeReadSnapshotCall(document),
            cancellationToken);
    }

    public static LBTask<ScopeExitSafePointResponse> RequestExitSafePointAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeExitSafePointCall, ScopeExitSafePointResponse>(
            new ScopeExitSafePointCall(),
            cancellationToken);
    }

    public static LBTask<ScopeInitializeResponse> RequestInitializeAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeInitializeCall, ScopeInitializeResponse>(new ScopeInitializeCall());
    }

    public static LBTask<ScopePostBuildResponse> RequestPostBuildAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopePostBuildCall, ScopePostBuildResponse>(new ScopePostBuildCall());
    }

    public static LBTask<ScopeRuntimeStartResponse> RequestRuntimeStartAsync(this ScopeRuntime scope)
    {
        return scope.EnqueueControlCall<ScopeRuntimeStartCall, ScopeRuntimeStartResponse>(new ScopeRuntimeStartCall());
    }

    public static LBTask<ScopeCaptureDiagnosticsResponse> RequestCaptureDiagnosticsAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeCaptureDiagnosticsCall, ScopeCaptureDiagnosticsResponse>(
            new ScopeCaptureDiagnosticsCall(),
            cancellationToken);
    }

    public static LBTask<ScopePrewarmResponse> RequestPrewarmAsync(
        this ScopeRuntime scope,
        LayerPrewarmOptions options,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopePrewarmCall, ScopePrewarmResponse>(
            new ScopePrewarmCall(options),
            cancellationToken);
    }

    public static LBTask<ScopeFreezeRuntimeRegistriesResponse> RequestFreezeRuntimeRegistriesAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeFreezeRuntimeRegistriesCall, ScopeFreezeRuntimeRegistriesResponse>(
            new ScopeFreezeRuntimeRegistriesCall(),
            cancellationToken);
    }

    public static LBTask<ScopeRecompileTimerPlansResponse> RequestRecompileTimerPlansAsync(
        this ScopeRuntime scope,
        CancellationToken cancellationToken = default)
    {
        return scope.EnqueueControlCall<ScopeRecompileTimerPlansCall, ScopeRecompileTimerPlansResponse>(
            new ScopeRecompileTimerPlansCall(),
            cancellationToken);
    }
}
