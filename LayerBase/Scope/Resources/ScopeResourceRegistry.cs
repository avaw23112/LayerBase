using System;
using System.Collections.Generic;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

internal sealed class ScopeResourceRegistry
{
    private readonly List<IGeneratedScopeResourceConsumer> _consumers = new();
    private readonly List<Action> _unbindActions = new();
    private object[] _exports = Array.Empty<object>();
    private bool _closed;

    public void Initialize(
        object[] scopeObjects,
        ScopeResourcePlan plan)
    {
        if (_closed)
            throw new InvalidOperationException("Scope resource registry is already closed.");
        if (scopeObjects == null) throw new ArgumentNullException(nameof(scopeObjects));
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        _consumers.Clear();
        _exports = new object[plan.Exports.Length];

        for (int i = 0; i < plan.Exports.Length; i++)
        {
            ScopeResourceExportPlan export = plan.Exports[i];
            var publisher = (IGeneratedScopeResourcePublisher)scopeObjects[export.ProviderObjectSlot];
            object value = publisher.GetPublishedResource(export.ProviderLocalSlot);
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"Scope resource provider '{publisher.GetType().FullName}' returned null for local export slot {export.ProviderLocalSlot}.");
            }

            _exports[export.ExportSlot] = value;
        }

        for (int i = 0; i < plan.Imports.Length; i++)
        {
            ScopeResourceImportPlan import = plan.Imports[i];
            var consumer = (IGeneratedScopeResourceConsumer)scopeObjects[import.ConsumerObjectSlot];
            consumer.BindScopeResource(import.ConsumerLocalSlot, _exports[import.ExportSlot]);

            if (!_consumers.Contains(consumer))
            {
                _consumers.Add(consumer);
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
        _exports = Array.Empty<object>();
    }
}
