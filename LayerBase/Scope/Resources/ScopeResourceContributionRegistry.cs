using System;
using System.Collections.Generic;

namespace LayerBase.Scope.Resources;

public static class ScopeResourceContributionRegistry
{
    private static readonly object s_gate = new();
    private static ScopeResourceExportContribution[] s_exports = Array.Empty<ScopeResourceExportContribution>();
    private static ScopeResourceImportContribution[] s_imports = Array.Empty<ScopeResourceImportContribution>();

    public static void Register(
        ScopeResourceExportContribution[] exports,
        ScopeResourceImportContribution[] imports)
    {
        exports ??= Array.Empty<ScopeResourceExportContribution>();
        imports ??= Array.Empty<ScopeResourceImportContribution>();

        lock (s_gate)
        {
            if (exports.Length > 0)
            {
                var nextExports = new ScopeResourceExportContribution[s_exports.Length + exports.Length];
                Array.Copy(s_exports, nextExports, s_exports.Length);
                Array.Copy(exports, 0, nextExports, s_exports.Length, exports.Length);
                s_exports = nextExports;
            }

            if (imports.Length > 0)
            {
                var nextImports = new ScopeResourceImportContribution[s_imports.Length + imports.Length];
                Array.Copy(s_imports, nextImports, s_imports.Length);
                Array.Copy(imports, 0, nextImports, s_imports.Length, imports.Length);
                s_imports = nextImports;
            }
        }
    }

    internal static ScopeResourceContributionSnapshot CollectFor(IReadOnlyList<object> candidates)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));

        var candidateTypes = new HashSet<RuntimeTypeHandle>();
        for (int i = 0; i < candidates.Count; i++)
        {
            candidateTypes.Add(candidates[i].GetType().TypeHandle);
        }

        lock (s_gate)
        {
            var exports = new List<ScopeResourceExportContribution>();
            for (int i = 0; i < s_exports.Length; i++)
            {
                ScopeResourceExportContribution export = s_exports[i];
                if (candidateTypes.Contains(export.ProviderType))
                {
                    exports.Add(export);
                }
            }

            var imports = new List<ScopeResourceImportContribution>();
            for (int i = 0; i < s_imports.Length; i++)
            {
                ScopeResourceImportContribution import = s_imports[i];
                if (candidateTypes.Contains(import.ConsumerType))
                {
                    imports.Add(import);
                }
            }

            return new ScopeResourceContributionSnapshot(exports.ToArray(), imports.ToArray());
        }
    }
}

internal readonly struct ScopeResourceContributionSnapshot
{
    public ScopeResourceContributionSnapshot(
        ScopeResourceExportContribution[] exports,
        ScopeResourceImportContribution[] imports)
    {
        Exports = exports ?? throw new ArgumentNullException(nameof(exports));
        Imports = imports ?? throw new ArgumentNullException(nameof(imports));
    }

    public ScopeResourceExportContribution[] Exports { get; }

    public ScopeResourceImportContribution[] Imports { get; }
}
