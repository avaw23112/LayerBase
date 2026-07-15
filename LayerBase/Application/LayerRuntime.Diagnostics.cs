using LayerBase.Scope;
using LayerBase.Core.Event;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public RuntimeDiagnosticsSnapshot CaptureDiagnostics()
    {
        if (_disposed || _state is RuntimeState.Disposing or RuntimeState.Disposed)
            throw new ObjectDisposedException(nameof(LayerRuntime));
        if (_scopeHost.HasWorkerScopes)
            throw new InvalidOperationException(
                "Synchronous diagnostics capture cannot coordinate WorkerScope safely. Use CaptureDiagnosticsAsync.");
        if (!_scopeHost.MainScope.IsOwnerThread)
            throw new InvalidOperationException(
                "Synchronous diagnostics capture must run on the MainScope owner thread.");

        return CaptureDiagnosticsCore();
    }

    public async ValueTask<RuntimeDiagnosticsSnapshot> CaptureDiagnosticsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_disposed || _state is RuntimeState.Disposing or RuntimeState.Disposed)
            throw new ObjectDisposedException(nameof(LayerRuntime));

        cancellationToken.ThrowIfCancellationRequested();

        if (!_scopeHost.HasWorkerScopes)
            return CaptureDiagnosticsCore();

        return await CaptureDiagnosticsAsyncCore(cancellationToken);
    }

    private RuntimeDiagnosticsSnapshot CaptureDiagnosticsCore()
    {
        var scopes = _scopeHost.Scopes
                               .Select(static scope => scope.CaptureDiagnostics())
                               .OrderBy(static scope => scope.ScopeId)
                               .ToArray();

        return new RuntimeDiagnosticsSnapshot(
            _id,
            _generation,
            _state,
            DateTime.UtcNow.Ticks,
            scopes,
            MainActorRuntime.CaptureDiagnostics(),
            CapturePayloadDiagnostics());
    }

    private async ValueTask<RuntimeDiagnosticsSnapshot> CaptureDiagnosticsAsyncCore(
        CancellationToken cancellationToken)
    {
        var scopes = new List<ScopeDiagnosticsSnapshot>(_scopeHost.Scopes.Count);
        foreach (var scope in _scopeHost.Scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ScopeDiagnosticsSnapshot snapshot;
            if (scope.Options.Threading == ScopeThreadingMode.Worker)
            {
                var response = await scope.RequestCaptureDiagnosticsAsync(cancellationToken);
                if (response.Result != ScopeControlResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Scope `{scope.Descriptor.Name}` rejected diagnostics capture.");
                }

                snapshot = response.Snapshot;
            }
            else
            {
                snapshot = scope.CaptureDiagnostics();
            }

            scopes.Add(snapshot);
        }

        return new RuntimeDiagnosticsSnapshot(
            _id,
            _generation,
            _state,
            DateTime.UtcNow.Ticks,
            scopes.OrderBy(static scope => scope.ScopeId).ToArray(),
            MainActorRuntime.CaptureDiagnostics(),
            CapturePayloadDiagnostics());
    }

    private PayloadDiagnosticsSnapshot CapturePayloadDiagnostics()
    {
        var stores = new HashSet<IEventStore>();
        foreach (var scope in _scopeHost.Scopes)
            scope.Transport.AddPayloadStoresTo(stores);

        return EventPayloadStorage.CaptureDiagnostics(stores);
    }
}
