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
            if ((uint)export.ProviderObjectSlot >= (uint)scopeObjects.Length)
            {
                throw new InvalidOperationException(
                    $"Scope resource provider object slot {export.ProviderObjectSlot} is outside scope object length {scopeObjects.Length}.");
            }

            if ((uint)export.ExportSlot >= (uint)_exports.Length)
            {
                throw new InvalidOperationException(
                    $"Scope resource export slot {export.ExportSlot} is outside export table length {_exports.Length}.");
            }

            if (export.ProviderLocalSlot < 0)
            {
                throw new InvalidOperationException(
                    $"Scope resource provider local slot {export.ProviderLocalSlot} is invalid.");
            }

            if (scopeObjects[export.ProviderObjectSlot] is not IGeneratedScopeResourcePublisher publisher)
            {
                throw new InvalidOperationException(
                    $"Scope resource provider object at slot {export.ProviderObjectSlot} does not implement {nameof(IGeneratedScopeResourcePublisher)}.");
            }

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
            if ((uint)import.ConsumerObjectSlot >= (uint)scopeObjects.Length)
            {
                throw new InvalidOperationException(
                    $"Scope resource consumer object slot {import.ConsumerObjectSlot} is outside scope object length {scopeObjects.Length}.");
            }

            if ((uint)import.ExportSlot >= (uint)_exports.Length)
            {
                throw new InvalidOperationException(
                    $"Scope resource import references export slot {import.ExportSlot}, outside export table length {_exports.Length}.");
            }

            if (import.ConsumerLocalSlot < 0)
            {
                throw new InvalidOperationException(
                    $"Scope resource consumer local slot {import.ConsumerLocalSlot} is invalid.");
            }

            if (scopeObjects[import.ConsumerObjectSlot] is not IGeneratedScopeResourceConsumer consumer)
            {
                throw new InvalidOperationException(
                    $"Scope resource consumer object at slot {import.ConsumerObjectSlot} does not implement {nameof(IGeneratedScopeResourceConsumer)}.");
            }

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

    public void CloseAndUnbind(Action<Exception, object>? report = null)
    {
        _closed = true;

        for (int i = 0; i < _consumers.Count; i++)
        {
            IGeneratedScopeResourceConsumer consumer = _consumers[i];
            try
            {
                consumer.UnbindScopeResources();
            }
            catch (Exception exception)
            {
                report?.Invoke(exception, consumer);
            }
        }

        _consumers.Clear();

        for (int i = 0; i < _unbindActions.Count; i++)
        {
            Action unbind = _unbindActions[i];
            try
            {
                unbind();
            }
            catch (Exception exception)
            {
                report?.Invoke(exception, unbind);
            }
        }

        _unbindActions.Clear();
        _exports = Array.Empty<object>();
    }
}
