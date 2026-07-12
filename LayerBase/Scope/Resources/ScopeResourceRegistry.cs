using System;
using System.Collections.Generic;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

internal sealed class ScopeResourceRegistry
{
    private readonly Dictionary<int, object> _exportsById = new();
    private readonly Dictionary<(RuntimeTypeHandle ProviderType, string LocalKey), PublishedExport> _exportsByKey = new();
    private readonly List<IGeneratedScopeResourceConsumer> _consumers = new();
    private readonly List<Action> _unbindActions = new();
    private bool _closed;

    public void Initialize(
        IReadOnlyList<IGeneratedScopeResourcePublisher> publishers,
        IReadOnlyList<IGeneratedScopeResourceConsumer> consumers,
        ScopeResourceExportContribution[] exportContributions,
        ScopeResourceImportContribution[] importContributions)
    {
        if (_closed)
            throw new InvalidOperationException("Scope resource registry is already closed.");

        _consumers.Clear();
        _consumers.AddRange(consumers);

        _exportsById.Clear();
        _exportsByKey.Clear();

        for (int i = 0; i < publishers.Count; i++)
        {
            IGeneratedScopeResourcePublisher publisher = publishers[i];
            RuntimeTypeHandle publisherType = publisher.GetType().TypeHandle;

            for (int j = 0; j < exportContributions.Length; j++)
            {
                ScopeResourceExportContribution export = exportContributions[j];
                if (!export.ProviderType.Equals(publisherType))
                {
                    continue;
                }

                object value = publisher.GetPublishedResource(export.ProviderLocalSlot);
                if (value == null)
                {
                    throw new InvalidOperationException(
                        $"Scope resource provider '{publisher.GetType().FullName}' returned null for export id {export.ExportId}.");
                }

                var key = (export.ProviderType, export.LocalKey);
                if (_exportsByKey.ContainsKey(key))
                {
                    Type? providerType = Type.GetTypeFromHandle(export.ProviderType);
                    throw new InvalidOperationException(
                        $"Scope resource provider conflict for providerType '{providerType?.FullName ?? "<unknown>"}' and localKey '{export.LocalKey}'.");
                }

                _exportsById[export.ExportId] = value;
                _exportsByKey[key] = new PublishedExport(export, value);
            }
        }

        for (int consumerIndex = 0; consumerIndex < _consumers.Count; consumerIndex++)
        {
            IGeneratedScopeResourceConsumer consumer = _consumers[consumerIndex];
            RuntimeTypeHandle consumerType = consumer.GetType().TypeHandle;

            for (int importIndex = 0; importIndex < importContributions.Length; importIndex++)
            {
                ScopeResourceImportContribution import = importContributions[importIndex];
                if (!import.ConsumerType.Equals(consumerType))
                {
                    continue;
                }

                var key = (import.ProviderType, import.LocalKey);
                if (!_exportsByKey.TryGetValue(key, out PublishedExport export))
                {
                    Type? providerType = Type.GetTypeFromHandle(import.ProviderType);
                    throw new InvalidOperationException(
                        $"Scope resource consumer '{consumer.GetType().FullName}' could not find a published scope resource " +
                        $"for providerType '{providerType?.FullName ?? "<unknown>"}' and localKey '{import.LocalKey}'.");
                }

                Type? requestedType = Type.GetTypeFromHandle(import.RequestedResourceType);
                if (requestedType != null && !requestedType.IsInstanceOfType(export.Value))
                {
                    Type? providerType = Type.GetTypeFromHandle(import.ProviderType);
                    throw new InvalidOperationException(
                        $"Scope resource consumer '{consumer.GetType().FullName}' cannot read provider '{providerType?.FullName ?? "<unknown>"}.{import.LocalKey}' " +
                        $"as '{requestedType.FullName}'.");
                }

                consumer.BindScopeResource(import.ConsumerLocalSlot, export.Value);
            }
        }
    }

    public void TrackUnbindAction(Action unbind)
    {
        if (unbind == null) throw new ArgumentNullException(nameof(unbind));
        _unbindActions.Add(unbind);
    }

    public void CloseAndUnbind()
    {
        _closed = true;

        for (int i = 0; i < _consumers.Count; i++)
        {
            try
            {
                _consumers[i].UnbindScopeResources();
            }
            catch
            {
            }
        }

        _consumers.Clear();

        for (int i = 0; i < _unbindActions.Count; i++)
        {
            try
            {
                _unbindActions[i]();
            }
            catch
            {
            }
        }

        _unbindActions.Clear();
        _exportsById.Clear();
        _exportsByKey.Clear();
    }

    private readonly struct PublishedExport
    {
        public PublishedExport(ScopeResourceExportContribution contribution, object value)
        {
            Contribution = contribution;
            Value = value;
        }

        public ScopeResourceExportContribution Contribution { get; }
        public object Value { get; }
    }
}
