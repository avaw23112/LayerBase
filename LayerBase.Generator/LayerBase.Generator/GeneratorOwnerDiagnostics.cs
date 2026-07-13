using Microsoft.CodeAnalysis;

namespace LayerBase.Generator;

internal static class GeneratorOwnerDiagnostics
{
    public static readonly DiagnosticDescriptor GenericOwnerNotSupported = new(
        id: "LBG413",
        title: "Generic generated owner is not supported",
        messageFormat:
            "Type '{0}' or one of its containing types declares generic parameters. [Provide], [From] and [Mount] require a non-generic partial owner.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedMountOwnerNotSupported = new(
        id: "LBG414",
        title: "Nested mount owner is not supported",
        messageFormat:
            "Type '{0}' is nested. [Mount] requires a top-level non-generic partial owner.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static bool HasGenericContainingType(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsNestedType(INamedTypeSymbol type)
    {
        return type.ContainingType != null;
    }
}
