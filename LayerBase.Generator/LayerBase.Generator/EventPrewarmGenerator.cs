using System;
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
public sealed class EventPrewarmGenerator : IIncrementalGenerator
{
    private const string PrewarmEventAttributeName = "LayerBase.Core.Event.PrewarmEventAttribute";
    private const string EventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandler`1";
    private const string EventHandlerAsyncMetadataName = "LayerBase.Core.EventHandler.IEventHandlerAsync`1";
    private const string EventMetaDataBaseName = "LayerBase.Event.EventMetaData.EventMetaData`1";

    private const string SubscribeAttributeName = "LayerBase.Core.Event.SubscribeAttribute";
    private const string SubscribeNotifyAttributeName = "LayerBase.Core.Event.SubscribeNotifyAttribute";
    private const string SubscribeFlowAttributeName = "LayerBase.Core.Event.SubscribeFlowAttribute";
    private const string SubscribeAsyncAttributeName = "LayerBase.Core.Event.SubscribeAsyncAttribute";
    private const string SubscribeParallelAttributeName = "LayerBase.Core.Event.SubscribeParallelAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Collect from [PrewarmEvent]
        var prewarmEventTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            PrewarmEventAttributeName,
            static (node, _) => node is StructDeclarationSyntax,
            static (ctx,  _) => ctx.TargetSymbol as INamedTypeSymbol
        ).Where(static s => s is not null);

        // 2. Collect from [Subscribe*] attributes on methods
        var subscribeEventTypes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
            static (ctx,  ct) => GetEventTypesFromMethodAttributes(ctx, ct)
        ).SelectMany(static (items, _) => items);

        // 3. Collect from IEventHandler<T> implementations
        var handlerEventTypes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            static (ctx,  ct) => GetEventTypesFromHandlerInterfaces(ctx, ct)
        ).SelectMany(static (items, _) => items);

        // 4. Collect from EventMetaData<T>
        var metaDataEventTypes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            static (ctx,  ct) => GetEventTypesFromMetaDataBases(ctx, ct)
        ).SelectMany(static (items, _) => items);

        var allCollected = prewarmEventTypes.Collect()
                                            .Combine(subscribeEventTypes.Collect())
                                            .Combine(handlerEventTypes.Collect())
                                            .Combine(metaDataEventTypes.Collect());

        context.RegisterSourceOutput(allCollected.Combine(context.CompilationProvider), static (spc, source) =>
        {
            var ((((prewarm, subscribe), handler), metadata), compilation) = source;

            var allTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var s in prewarm)
                if (s != null)
                    allTypes.Add(s);
            foreach (var s in subscribe)
                if (s != null)
                    allTypes.Add(s);
            foreach (var s in handler)
                if (s != null)
                    allTypes.Add(s);
            foreach (var s in metadata)
                if (s != null)
                    allTypes.Add(s);

            if (allTypes.Count == 0) return;

            var assemblyName = compilation.AssemblyName ?? "UnknownAssembly";
            var sourceText = GenerateSource(allTypes.OrderBy(t => t.ToDisplayString()).ToList(), assemblyName);
            spc.AddSource("LayerBasePrewarmInitializer.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        });
    }

    private static ImmutableArray<ITypeSymbol> GetEventTypesFromMethodAttributes(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(method, ct);
        if (symbol == null) return ImmutableArray<ITypeSymbol>.Empty;

        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name == SubscribeAttributeName ||
                name == SubscribeNotifyAttributeName ||
                name == SubscribeFlowAttributeName ||
                name == SubscribeAsyncAttributeName ||
                name == SubscribeParallelAttributeName)
            {
                if (symbol.Parameters.Length > 0)
                {
                    builder.Add(symbol.Parameters[0].Type);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ITypeSymbol> GetEventTypesFromHandlerInterfaces(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, ct);
        if (symbol == null) return ImmutableArray<ITypeSymbol>.Empty;

        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        foreach (var iface in symbol.AllInterfaces)
        {
            if (iface.IsGenericType)
            {
                var def = iface.OriginalDefinition.ToDisplayString();
                if (def == EventHandlerMetadataName || def == EventHandlerAsyncMetadataName)
                {
                    builder.Add(iface.TypeArguments[0]);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ITypeSymbol> GetEventTypesFromMetaDataBases(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, ct);
        if (symbol == null) return ImmutableArray<ITypeSymbol>.Empty;

        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        for (var current = symbol; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.OriginalDefinition.ToDisplayString() == EventMetaDataBaseName)
            {
                builder.Add(current.TypeArguments[0]);
            }
        }

        return builder.ToImmutable();
    }

    private static string GenerateSource(List<ITypeSymbol> eventTypes, string assemblyName)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using System;");
        builder.AppendLine("using LayerBase.Core.Event;");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Core.Event");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine($"    /// 自动生成的预热引导器 - 针对程序集: {assemblyName}");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public partial struct EventPrewarmBootstrapper");
        builder.AppendLine("    {");
        builder.AppendLine("        static EventPrewarmBootstrapper()");
        builder.AppendLine("        {");
        builder.AppendLine("            LayerBasePrewarmRegistry.Register((center, options) =>");
        builder.AppendLine("            {");

        foreach (var type in eventTypes)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.AppendLine($"                EventCenter.RegisterEventType<{typeName}>();");
            builder.AppendLine($"                center.PrewarmEvent<{typeName}>(in options);");
        }

        builder.AppendLine("            });");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
