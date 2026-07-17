using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class EventMetaDataGenerator : IIncrementalGenerator
{
    private const string EventMetaDataBaseName = "LayerBase.Event.EventMetaData.EventMetaData`1";
    private const string EventMetaDataRegistryName = "LayerBase.Event.EventMetaData.EventMetaDataRegistry";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var registrations = context.SyntaxProvider
                                   .CreateSyntaxProvider(
                                       static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                                       static (ctx, ct) => CreateRegistration(ctx, ct))
                                   .Where(static r => r is not null)
                                   .Select(static (r, _) => r!);

        var compilationAndRegistrations = context.CompilationProvider.Combine(registrations.Collect());

        context.RegisterSourceOutput(compilationAndRegistrations, static (spc, source) =>
        {
            var compilation = source.Left;
            var collected = source.Right;

            var eventMetaDataBase = compilation.GetTypeByMetadataName(EventMetaDataBaseName);
            var registryType = compilation.GetTypeByMetadataName(EventMetaDataRegistryName);

            if (eventMetaDataBase == null || registryType == null)
            {
                return;
            }

            List<MetaDataRegistration> validRegistrations = new();
            HashSet<INamedTypeSymbol> registeredEvents = new(SymbolEqualityComparer.Default);

            foreach (var registration in collected)
            {
                foreach (var diagnostic in registration.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (!registration.IsValid)
                {
                    continue;
                }

                if (registration.EventType is not INamedTypeSymbol eventTypeSymbol)
                {
                    continue;
                }

                if (ContainsGenericTypeInPartialChain(eventTypeSymbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.EventTypeOrContainingTypeCannotBeGeneric,
                        registration.MetaDataType.Locations.FirstOrDefault(),
                        eventTypeSymbol.ToDisplayString()));

                    continue;
                }

                if (!IsPartialTypeChain(eventTypeSymbol, out var nonPartialType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.EventTypeMustBePartial,
                        registration.MetaDataType.Locations.FirstOrDefault(),
                        nonPartialType?.ToDisplayString() ?? eventTypeSymbol.ToDisplayString()));

                    continue;
                }

                if (!registeredEvents.Add(eventTypeSymbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.DuplicateMetaDataForEventType,
                        registration.MetaDataType.Locations.FirstOrDefault(),
                        eventTypeSymbol.ToDisplayString()));

                    continue;
                }

                validRegistrations.Add(registration);
            }

            if (validRegistrations.Count == 0)
            {
                return;
            }

            var sourceText = GenerateSource(validRegistrations, compilation.AssemblyName ?? "Assembly");

            if (!string.IsNullOrEmpty(sourceText))
            {
                spc.AddSource(CreateHintName(compilation.AssemblyName), SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private static MetaDataRegistration? CreateRegistration(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(
            classDeclaration,
            cancellationToken);

        if (typeSymbol == null)
        {
            return null;
        }

        var eventMetaDataSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(EventMetaDataBaseName);

        if (eventMetaDataSymbol == null)
        {
            return null;
        }

        var eventType = GetEventType(typeSymbol, eventMetaDataSymbol);

        if (eventType == null)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var location = typeSymbol.Locations.FirstOrDefault();

        if (typeSymbol.IsAbstract)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.MetaDataCannotBeAbstract,
                location,
                typeSymbol.ToDisplayString()));
        }

        if (typeSymbol.TypeParameters.Length > 0)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.MetaDataCannotBeGeneric,
                location,
                typeSymbol.ToDisplayString()));
        }

        if (!HasAccessibleParameterlessConstructor(typeSymbol))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.MetaDataNeedsPublicParameterlessConstructor,
                location,
                typeSymbol.ToDisplayString()));
        }

        return new MetaDataRegistration(
            typeSymbol,
            eventType,
            diagnostics.ToImmutable());
    }

    private static INamedTypeSymbol? GetEventType(
        INamedTypeSymbol type,
        INamedTypeSymbol eventMetaDataSymbol)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol named &&
                named.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, eventMetaDataSymbol))
            {
                return named.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length != 0 || ctor.IsStatic)
            {
                continue;
            }

            if (ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
                typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartialTypeChain(
        INamedTypeSymbol type,
        out INamedTypeSymbol? nonPartialType)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (!IsPartial(current))
            {
                nonPartialType = current;
                return false;
            }
        }

        nonPartialType = null;
        return true;
    }

    private static bool ContainsGenericTypeInPartialChain(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateSource(
        IEnumerable<MetaDataRegistration> registrations,
        string assemblyName)
    {
        _ = assemblyName;

        var grouped = registrations
                      .Distinct(MetaDataRegistrationComparer.Instance)
                      .Where(r => r.EventType is not null)
                      .GroupBy(r => r.EventType!, SymbolEqualityComparer.Default)
                      .OrderBy(g => g.Key?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                      .ToList();

        if (grouped.Count == 0)
        {
            return string.Empty;
        }

        var sortedRegistrations = grouped
            .Select(g => g.Key)
            .Where(k => k != null)
            .Cast<INamedTypeSymbol>()
            .OrderBy(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToList();

        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// This file was generated by EventMetaDataGenerator.");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        foreach (var group in grouped)
        {
            if (group.Key is not INamedTypeSymbol eventType)
            {
                continue;
            }

            var metaData = group.First();

            var metaDataDisplay = metaData.MetaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var eventTypeDisplay = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var @namespace = eventType.ContainingNamespace is { IsGlobalNamespace: false } ns
                ? ns.ToDisplayString()
                : null;

            var namespaceIndent = string.IsNullOrEmpty(@namespace) ? 0 : 1;

            if (!string.IsNullOrEmpty(@namespace))
            {
                builder.Append("namespace ").Append(@namespace).AppendLine();
                builder.AppendLine("{");
            }

            var typeChain = GetTypeChain(eventType);

            for (var i = 0; i < typeChain.Count; i++)
            {
                AppendTypeDeclarationOpen(
                    builder,
                    typeChain[i],
                    indentLevel: namespaceIndent + i);
            }

            var staticCtorIndent = namespaceIndent + typeChain.Count;

            AppendIndent(builder, staticCtorIndent);
            builder.Append("static ").Append(eventType.Name).AppendLine("()");

            AppendIndent(builder, staticCtorIndent);
            builder.AppendLine("{");

            AppendIndent(builder, staticCtorIndent + 1);
            builder.Append("global::LayerBase.Event.EventMetaData.EventMetaDataAutoRegister<")
                   .Append(eventTypeDisplay)
                   .AppendLine(">.SetReplay(");

            AppendIndent(builder, staticCtorIndent + 2);
            builder.AppendLine("static () =>");

            AppendIndent(builder, staticCtorIndent + 2);
            builder.AppendLine("{");

            AppendIndent(builder, staticCtorIndent + 3);
            builder.Append("global::LayerBase.Event.EventMetaData.EventMetaDataRegistry.RegisterMetaData<")
                   .Append(eventTypeDisplay)
                   .Append(">(new ")
                   .Append(metaDataDisplay)
                   .AppendLine("());");

            AppendIndent(builder, staticCtorIndent + 2);
            builder.AppendLine("});");

            AppendIndent(builder, staticCtorIndent);
            builder.AppendLine("}");

            for (var i = typeChain.Count - 1; i >= 0; i--)
            {
                AppendIndent(builder, namespaceIndent + i);
                builder.AppendLine("}");
            }

            if (!string.IsNullOrEmpty(@namespace))
            {
                builder.AppendLine("}");
            }

            builder.AppendLine();
        }

        AppendAssemblyBootstrapper(builder, sortedRegistrations);

        return builder.ToString();
    }

    private static void AppendAssemblyBootstrapper(
        StringBuilder builder,
        IReadOnlyList<INamedTypeSymbol> eventTypes)
    {
        if (eventTypes.Count == 0)
            return;

        builder.AppendLine("namespace LayerBase.Event.EventMetaData");
        builder.AppendLine("{");
        builder.AppendLine("    internal static class EventMetaDataBootstrapper");
        builder.AppendLine("    {");
        builder.AppendLine("        static EventMetaDataBootstrapper()");
        builder.AppendLine("        {");
        builder.AppendLine("            global::LayerBase.Event.EventMetaData.EventMetaDataBootstrapRegistry.Register(");
        builder.AppendLine("                static () =>");
        builder.AppendLine("                {");

        for (int i = 0; i < eventTypes.Count; i++)
        {
            var display = eventTypes[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("                    global::LayerBase.Event.EventMetaData.EventMetaDataAutoRegister<");
            builder.Append(display);
            builder.AppendLine(">.EnsureInitialized();");
        }

        builder.AppendLine("                });");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
    }

    private static List<INamedTypeSymbol> GetTypeChain(INamedTypeSymbol eventType)
    {
        Stack<INamedTypeSymbol> stack = new();

        for (var current = eventType; current != null; current = current.ContainingType)
        {
            stack.Push(current);
        }

        return stack.ToList();
    }

    private static void AppendTypeDeclarationOpen(
        StringBuilder builder,
        INamedTypeSymbol type,
        int indentLevel)
    {
        AppendIndent(builder, indentLevel);

        if (type.IsStatic)
        {
            builder.Append("static ");
        }

        builder.Append("partial ")
               .Append(GetTypeKeyword(type))
               .Append(' ')
               .Append(type.Name)
               .AppendLine();

        AppendIndent(builder, indentLevel);
        builder.AppendLine("{");
    }

    private static string GetTypeKeyword(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is RecordDeclarationSyntax recordDeclaration)
            {
                if (recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
                {
                    return "record struct";
                }

                return "record";
            }

            if (reference.GetSyntax() is TypeDeclarationSyntax typeDeclaration)
            {
                if (typeDeclaration.Keyword.IsKind(SyntaxKind.StructKeyword))
                {
                    return "struct";
                }

                if (typeDeclaration.Keyword.IsKind(SyntaxKind.ClassKeyword))
                {
                    return "class";
                }

                if (typeDeclaration.Keyword.IsKind(SyntaxKind.InterfaceKeyword))
                {
                    return "interface";
                }
            }
        }

        return type.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Class => "class",
            TypeKind.Interface => "interface",
            _ => "class"
        };
    }

    private static void AppendIndent(
        StringBuilder builder,
        int indentLevel)
    {
        for (var i = 0; i < indentLevel; i++)
        {
            builder.Append("    ");
        }
    }

    private static string CreateHintName(string? assemblyName)
    {
        var sanitized = Sanitize(string.IsNullOrWhiteSpace(assemblyName) ? "Assembly" : assemblyName!);
        return $"{sanitized}.EventMetaData.g.cs";
    }

    private static string Sanitize(string value)
    {
        var sanitized = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        if (sanitized.Length == 0)
        {
            sanitized.Append("Assembly");
        }

        return sanitized.ToString();
    }

    private sealed record MetaDataRegistration(
        INamedTypeSymbol MetaDataType,
        INamedTypeSymbol EventType,
        ImmutableArray<Diagnostic> Diagnostics)
    {
        public bool IsValid => Diagnostics.IsDefaultOrEmpty;
    }

    private sealed class MetaDataRegistrationComparer : IEqualityComparer<MetaDataRegistration>
    {
        public static readonly MetaDataRegistrationComparer Instance = new();

        public bool Equals(MetaDataRegistration? x, MetaDataRegistration? y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }

            return SymbolEqualityComparer.Default.Equals(x.MetaDataType, y.MetaDataType);
        }

        public int GetHashCode(MetaDataRegistration obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.MetaDataType);
        }
    }

#pragma warning disable RS2008 // Enable analyzer release tracking
    private static class Diagnostics
    {
        private const string Category = "EventMetaDataGenerator";

        public static readonly DiagnosticDescriptor MetaDataNeedsPublicParameterlessConstructor =
            new(
                "LBG204",
                "Event metadata needs parameterless constructor",
                "Event metadata '{0}' must have a public or internal parameterless constructor",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MetaDataCannotBeAbstract =
            new(
                "LBG205",
                "Event metadata cannot be abstract",
                "Event metadata '{0}' cannot be abstract",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MetaDataCannotBeGeneric =
            new(
                "LBG206",
                "Event metadata cannot be generic",
                "Event metadata '{0}' cannot be generic when used for registration",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor EventTypeMustBePartial =
            new(
                "LBG207",
                "Event type must be partial",
                "Event type or containing type '{0}' must be partial to allow metadata registration",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor DuplicateMetaDataForEventType =
            new(
                "LBG208",
                "Duplicate event metadata registration",
                "Only one EventMetaData can target event type '{0}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor EventTypeOrContainingTypeCannotBeGeneric =
            new(
                "LBG209",
                "Event type cannot be nested in a generic type",
                "Event type '{0}' and its containing types must not be generic when using static-constructor metadata registration",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008
}
