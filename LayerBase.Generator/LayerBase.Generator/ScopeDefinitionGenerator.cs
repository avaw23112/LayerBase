using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ScopeDefinitionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> candidates =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is TypeDeclarationSyntax { BaseList: not null },
                    transform: static (syntaxContext, _) =>
                        syntaxContext.SemanticModel.GetDeclaredSymbol(
                            (TypeDeclarationSyntax)syntaxContext.Node)
                        as INamedTypeSymbol)
                .Where(static symbol => symbol != null)
                .Select(static (symbol, _) => symbol!);

        IncrementalValueProvider<
            (Compilation Compilation, ImmutableArray<INamedTypeSymbol> Symbols)>
            input = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(
            input,
            static (productionContext, value) =>
                Execute(
                    productionContext,
                    value.Compilation,
                    value.Symbols));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> symbols)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var models = new List<ScopeDefinitionModel>();

        for (int i = 0; i < symbols.Length; i++)
        {
            INamedTypeSymbol symbol = symbols[i];

            if (!seen.Add(symbol))
                continue;

            if (!ScopeDefinitionCodeGen.ImplementsScopeDefinition(symbol))
                continue;

            if (ScopeDefinitionCodeGen.TryCreateModel(
                    context,
                    compilation,
                    symbol,
                    reportDiagnostics: true,
                    out ScopeDefinitionModel model))
            {
                models.Add(model);
            }
        }

        ScopeDefinitionCodeGen.ReportLocalCollisions(context, models);
    }
}
