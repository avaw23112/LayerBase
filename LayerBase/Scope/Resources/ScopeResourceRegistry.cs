using System;
using System.Collections.Generic;
using System.Reflection;
using LayerBase.DI;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

internal sealed class ScopeResourceRegistry
{
    private readonly Dictionary<int, object> _exports = new();
    private readonly List<IGeneratedScopeResourceConsumer> _consumers = new();
    private bool _closed;

    public void Initialize(
        IReadOnlyList<IGeneratedScopeResourcePublisher> publishers,
        IReadOnlyList<IGeneratedScopeResourceConsumer> consumers,
        ScopeResourceExportContribution[] exportContributions)
    {
        if (_closed)
            throw new InvalidOperationException("Scope resource registry is already closed.");

        _consumers.Clear();
        _consumers.AddRange(consumers);

        _exports.Clear();
        for (int i = 0; i < publishers.Count; i++)
        {
            for (int j = 0; j < exportContributions.Length; j++)
            {
                if (exportContributions[j].ProviderType.Equals(publishers[i].GetType().TypeHandle))
                {
                    object value = publishers[i].GetPublishedResource(exportContributions[j].ExportId);
                    if (value == null)
                        throw new InvalidOperationException(
                            $"Scope resource provider '{publishers[i].GetType().FullName}' returned null for export id {exportContributions[j].ExportId}.");
                    _exports[exportContributions[j].ExportId] = value;
                }
            }
        }

        foreach (var consumer in _consumers)
        {
            foreach (var import in FindImports(consumer.GetType(), exportContributions))
            {
                if (_exports.TryGetValue(import.ExportId, out object? resource))
                {
                    consumer.BindScopeResource(import.ExportId, resource);
                }
            }
        }
    }

    public void CloseAndUnbind()
    {
        _closed = true;
        for (int i = 0; i < _consumers.Count; i++)
        {
            _consumers[i].UnbindScopeResources();
        }
        _consumers.Clear();
        _exports.Clear();
    }

    private static ScopeResourceExportContribution[] FindImports(
        Type consumerType,
        ScopeResourceExportContribution[] exports)
    {
        var matching = new List<ScopeResourceExportContribution>();
        foreach (var export in exports)
        {
            Type providerType = Type.GetTypeFromHandle(export.ProviderType);
            if (providerType != null && HasConsumerImport(consumerType, providerType, export.LocalKey))
            {
                matching.Add(export);
            }
        }
        return matching.ToArray();
    }

    private static bool HasConsumerImport(Type consumerType, Type providerType, string localKey)
    {
        foreach (var field in consumerType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            var from = field.GetCustomAttribute<FromAttribute>();
            if (from != null && from.ProviderType == providerType && from.LocalKey == localKey)
                return true;
        }
        foreach (var prop in consumerType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            var from = prop.GetCustomAttribute<FromAttribute>();
            if (from != null && from.ProviderType == providerType && from.LocalKey == localKey)
                return true;
        }
        return false;
    }
}
