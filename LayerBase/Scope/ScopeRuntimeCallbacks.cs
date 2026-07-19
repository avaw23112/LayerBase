using LayerBase.Core.Event;

namespace LayerBase.Scope;

internal delegate void ScopeFaultHandler(in ScopeFaultRecord fault);

internal delegate void ScopeDelayRegistryChangedHandler(int scopeId);

internal delegate void ScopeLayerEventErrorHandler(
    int layerIndex,
    string source,
    string eventName,
    Exception exception);

internal delegate void ScopeServicesDisposeHandler(int scopeId);

internal delegate ToolDiagnosticsSnapshot ScopeToolDiagnosticsProvider();

internal delegate bool ScopeSystemCallHandler(
    ScopeRuntime scope,
    in ScopeCallEnvelope envelope,
    EventPayloadStorage payloadStorage);

internal delegate bool ScopeSystemEventHandler(
    ScopeRuntime scope,
    in ScopeEventEnvelope envelope,
    EventPayloadStorage payloadStorage);

internal sealed class ScopeRuntimeCallbacks
{
    private static readonly ScopeFaultHandler DetachedFault = static (in ScopeFaultRecord _) => { };
    private static readonly ScopeDelayRegistryChangedHandler DetachedDelayRegistryChanged = static _ => { };
    private static readonly ScopeLayerEventErrorHandler DetachedLayerEventError = static (_, _, _, _) => { };
    private static readonly ScopeServicesDisposeHandler DetachedDisposeServices = static _ => { };

    public ScopeRuntimeCallbacks(
        ScopeFaultHandler fault,
        ScopeDelayRegistryChangedHandler delayRegistryChanged,
        ScopeLayerEventErrorHandler layerEventError,
        ScopeServicesDisposeHandler disposeServices,
        ScopeSystemCallHandler? systemCall = null,
        ScopeSystemEventHandler? systemEvent = null,
        ScopeToolDiagnosticsProvider? toolDiagnostics = null)
    {
        Fault = fault ?? throw new ArgumentNullException(nameof(fault));
        DelayRegistryChanged = delayRegistryChanged ?? throw new ArgumentNullException(nameof(delayRegistryChanged));
        LayerEventError = layerEventError ?? throw new ArgumentNullException(nameof(layerEventError));
        DisposeServices = disposeServices ?? throw new ArgumentNullException(nameof(disposeServices));
        ThrowIfMulticast(systemCall, nameof(systemCall));
        ThrowIfMulticast(systemEvent, nameof(systemEvent));
        SystemCall = systemCall;
        SystemEvent = systemEvent;
        ToolDiagnostics = toolDiagnostics;
    }

    public ScopeFaultHandler Fault { get; private set; }

    public ScopeDelayRegistryChangedHandler DelayRegistryChanged { get; private set; }

    public ScopeLayerEventErrorHandler LayerEventError { get; private set; }

    public ScopeServicesDisposeHandler DisposeServices { get; private set; }

    public ScopeSystemCallHandler? SystemCall { get; private set; }

    public ScopeSystemEventHandler? SystemEvent { get; private set; }

    public ScopeToolDiagnosticsProvider? ToolDiagnostics { get; private set; }

    public void Detach()
    {
        Fault = DetachedFault;
        DelayRegistryChanged = DetachedDelayRegistryChanged;
        LayerEventError = DetachedLayerEventError;
        DisposeServices = DetachedDisposeServices;
        SystemCall = null;
        SystemEvent = null;
        ToolDiagnostics = null;
    }

    private static void ThrowIfMulticast(Delegate? handler, string parameterName)
    {
        if (handler != null && handler.GetInvocationList().Length != 1)
        {
            throw new ArgumentException("System route callbacks must be single-cast.", parameterName);
        }
    }
}
