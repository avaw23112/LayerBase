using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace LayerBase.Generator;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SharedFieldFixer))]
[Shared]
public class SharedFieldFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LBG402");

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var fieldDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                                   .OfType<FieldDeclarationSyntax>().FirstOrDefault();
        if (fieldDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Sync ScopeRead view with [Publish] source",
                c => FixFieldTypeAsync(context.Document, fieldDeclaration, diagnostic, c),
                nameof(SharedFieldFixer)),
            diagnostic);
    }

    private async Task<Document> FixFieldTypeAsync(Document   document,   FieldDeclarationSyntax field,
                                                   Diagnostic diagnostic, CancellationToken      cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var message = diagnostic.GetMessage();
        var targetType = "object";
        var lastQuoteIndex = message.LastIndexOf('\'');
        var prevQuoteIndex = message.LastIndexOf('\'', lastQuoteIndex - 1);
        if (lastQuoteIndex > prevQuoteIndex && prevQuoteIndex != -1)
            targetType = message.Substring(prevQuoteIndex + 1, lastQuoteIndex - prevQuoteIndex - 1);

        var newType = SyntaxFactory.ParseTypeName($"LayerBase.Scope.ScopeRead<{targetType}>");
        var newField = field.WithDeclaration(field.Declaration.WithType(newType));

        var newRoot = root.ReplaceNode(field, newField.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }
}
