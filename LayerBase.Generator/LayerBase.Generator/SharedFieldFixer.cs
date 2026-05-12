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

        // Extract the expected type from the diagnostic message if possible, or we could just leave it to the user.
        // But better: our analyzer reported the target type in the diagnostic properties (if we had set them).
        // Since we didn't set properties yet, I'll update the analyzer to include them, 
        // but for now, I'll just provide a generic "Sync type with Public source" action.

        context.RegisterCodeFix(
            CodeAction.Create(
                "Sync type with [Provide] source",
                c => FixFieldTypeAsync(context.Document, fieldDeclaration, diagnostic, c),
                nameof(SharedFieldFixer)),
            diagnostic);
    }

    private async Task<Document> FixFieldTypeAsync(Document   document,   FieldDeclarationSyntax field,
                                                   Diagnostic diagnostic, CancellationToken      cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // In a real implementation, we would use the diagnostic's properties to get the exact type name.
        // For this demo, let's assume we can parse it from the message or we need to update Analyzer.
        // I will just change the type to a placeholder if I can't find it, 
        // but let's try to extract it from the message: "Key 'x' is consumed as 'A' but published as 'B'"
        var message = diagnostic.GetMessage();
        var targetType = "object";
        var lastQuoteIndex = message.LastIndexOf('\'');
        var prevQuoteIndex = message.LastIndexOf('\'', lastQuoteIndex - 1);
        if (lastQuoteIndex > prevQuoteIndex && prevQuoteIndex != -1)
            targetType = message.Substring(prevQuoteIndex + 1, lastQuoteIndex - prevQuoteIndex - 1);

        var newType = SyntaxFactory.ParseTypeName(targetType);
        var newField = field.WithDeclaration(field.Declaration.WithType(newType));

        var newRoot = root.ReplaceNode(field, newField.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }
}