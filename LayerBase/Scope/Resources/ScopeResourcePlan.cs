using System;
using System.Collections.Generic;
using System.Linq;

namespace LayerBase.Scope.Resources;

internal sealed class ScopeResourcePlan
{
    public static readonly ScopeResourcePlan Empty = new(
        Array.Empty<ScopeResourceExportPlan>(),
        Array.Empty<ScopeResourceImportPlan>());

    public ScopeResourcePlan(
        ScopeResourceExportPlan[] exports,
        ScopeResourceImportPlan[] imports)
    {
        Exports = exports ?? throw new ArgumentNullException(nameof(exports));
        Imports = imports ?? throw new ArgumentNullException(nameof(imports));
    }

    public ScopeResourceExportPlan[] Exports { get; }

    public ScopeResourceImportPlan[] Imports { get; }

    public bool IsEmpty => Exports.Length == 0 && Imports.Length == 0;
}

internal readonly struct ScopeResourceExportPlan
{
    public ScopeResourceExportPlan(
        int providerObjectSlot,
        int providerLocalSlot,
        int exportSlot)
    {
        ProviderObjectSlot = providerObjectSlot;
        ProviderLocalSlot = providerLocalSlot;
        ExportSlot = exportSlot;
    }

    public int ProviderObjectSlot { get; }

    public int ProviderLocalSlot { get; }

    public int ExportSlot { get; }
}

internal readonly struct ScopeResourceImportPlan
{
    public ScopeResourceImportPlan(
        int consumerObjectSlot,
        int consumerLocalSlot,
        int exportSlot)
    {
        ConsumerObjectSlot = consumerObjectSlot;
        ConsumerLocalSlot = consumerLocalSlot;
        ExportSlot = exportSlot;
    }

    public int ConsumerObjectSlot { get; }

    public int ConsumerLocalSlot { get; }

    public int ExportSlot { get; }
}

internal readonly struct ScopeResourceObjectCandidate
{
    public ScopeResourceObjectCandidate(RuntimeTypeHandle objectType, int objectSlot)
    {
        ObjectType = objectType;
        ObjectSlot = objectSlot;
    }

    public RuntimeTypeHandle ObjectType { get; }

    public int ObjectSlot { get; }
}

internal static class ScopeResourcePlanBuilder
{
    public static ScopeResourcePlan Build(
        IReadOnlyList<ScopeResourceObjectCandidate> candidates,
        IReadOnlyList<ScopeResourceExportContribution> exportContributions,
        IReadOnlyList<ScopeResourceImportContribution> importContributions)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        exportContributions ??= Array.Empty<ScopeResourceExportContribution>();
        importContributions ??= Array.Empty<ScopeResourceImportContribution>();

        var objectSlotsByType = new Dictionary<RuntimeTypeHandle, int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            ScopeResourceObjectCandidate candidate = candidates[i];
            if (candidate.ObjectSlot < 0)
            {
                throw new InvalidOperationException(
                    $"Scope resource candidate '{GetTypeName(candidate.ObjectType)}' has invalid object slot {candidate.ObjectSlot}.");
            }

            if (objectSlotsByType.ContainsKey(candidate.ObjectType))
            {
                throw new InvalidOperationException(
                    $"Duplicate scope resource candidate for object type '{GetTypeName(candidate.ObjectType)}'.");
            }

            objectSlotsByType.Add(candidate.ObjectType, candidate.ObjectSlot);
        }

        var exports = new List<ScopeResourceExportPlan>();
        var exportSlotsByKey = new Dictionary<(RuntimeTypeHandle ProviderType, string LocalKey), int>();

        for (int i = 0; i < exportContributions.Count; i++)
        {
            ScopeResourceExportContribution export = exportContributions[i];
            if (!objectSlotsByType.TryGetValue(export.ProviderType, out int providerObjectSlot))
            {
                continue;
            }

            if (export.ProviderLocalSlot < 0)
            {
                throw new InvalidOperationException(
                    $"Scope resource provider '{GetTypeName(export.ProviderType)}' has invalid local slot {export.ProviderLocalSlot}.");
            }

            _ = RequireType(export.DeclaredResourceType, "declared resource");

            var key = (export.ProviderType, export.LocalKey);
            if (exportSlotsByKey.ContainsKey(key))
            {
                Type? providerType = Type.GetTypeFromHandle(export.ProviderType);
                throw new InvalidOperationException(
                    $"Scope resource provider conflict for providerType '{providerType?.FullName ?? "<unknown>"}' and localKey '{export.LocalKey}'.");
            }

            int exportSlot = exports.Count;
            exportSlotsByKey.Add(key, exportSlot);
            exports.Add(new ScopeResourceExportPlan(
                providerObjectSlot,
                export.ProviderLocalSlot,
                exportSlot));
        }

        var imports = new List<ScopeResourceImportPlan>();
        for (int i = 0; i < importContributions.Count; i++)
        {
            ScopeResourceImportContribution import = importContributions[i];
            if (!objectSlotsByType.TryGetValue(import.ConsumerType, out int consumerObjectSlot))
            {
                continue;
            }

            if (import.ConsumerLocalSlot < 0)
            {
                throw new InvalidOperationException(
                    $"Scope resource consumer '{GetTypeName(import.ConsumerType)}' has invalid local slot {import.ConsumerLocalSlot}.");
            }

            var key = (import.ProviderType, import.LocalKey);
            if (!exportSlotsByKey.TryGetValue(key, out int exportSlot))
            {
                Type? providerType = Type.GetTypeFromHandle(import.ProviderType);
                Type? consumerType = Type.GetTypeFromHandle(import.ConsumerType);
                throw new InvalidOperationException(
                    $"Scope resource consumer '{consumerType?.FullName ?? "<unknown>"}' could not find a published scope resource " +
                    $"for providerType '{providerType?.FullName ?? "<unknown>"}' and localKey '{import.LocalKey}'.");
            }

            ScopeResourceExportContribution export = exportContributions.First(e =>
                e.ProviderType.Equals(import.ProviderType) &&
                string.Equals(e.LocalKey, import.LocalKey, StringComparison.Ordinal));
            Type declaredType = RequireType(export.DeclaredResourceType, "declared resource");
            Type requestedType = RequireType(import.RequestedResourceType, "requested resource");
            if (!requestedType.IsAssignableFrom(declaredType))
            {
                throw new InvalidOperationException(
                    $"Scope resource '{import.LocalKey}' from provider '{GetTypeName(import.ProviderType)}' declared type '{declaredType.FullName}' is not assignable to requested type '{requestedType.FullName}' for consumer '{GetTypeName(import.ConsumerType)}'.");
            }

            imports.Add(new ScopeResourceImportPlan(
                consumerObjectSlot,
                import.ConsumerLocalSlot,
                exportSlot));
        }

        return exports.Count == 0 && imports.Count == 0
            ? ScopeResourcePlan.Empty
            : new ScopeResourcePlan(exports.ToArray(), imports.ToArray());
    }

    private static Type RequireType(RuntimeTypeHandle handle, string role)
    {
        return Type.GetTypeFromHandle(handle)
            ?? throw new InvalidOperationException($"Scope resource {role} type handle '{handle}' cannot be resolved.");
    }

    private static string GetTypeName(RuntimeTypeHandle handle)
    {
        return Type.GetTypeFromHandle(handle)?.FullName ?? "<unknown>";
    }
}
