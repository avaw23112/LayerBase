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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MountPartialFixer))]
[Shared]
public class MountPartialFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => 
        ImmutableArray.Create("LBMOUNT001", "LBMOUNT006");

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

        var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                                   .OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Make class partial",
                c => MakePartialAsync(context.Document, classDeclaration, c),
                nameof(MountPartialFixer)),
            diagnostic);
    }

    private async Task<Document> MakePartialAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
        var newModifiers = classDeclaration.Modifiers.Add(partialKeyword);
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }
}
