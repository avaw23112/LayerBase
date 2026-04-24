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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallSignatureFixer))]
[Shared]
public class CallSignatureFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LBG301");

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

        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                                    .OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Fix [Call] signature",
                c => FixCallSignatureAsync(context.Document, methodDeclaration, c),
                nameof(CallSignatureFixer)),
            diagnostic);
    }

    private async Task<Document> FixCallSignatureAsync(Document          document, MethodDeclarationSyntax method,
                                                       CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // 1. Ensure at least one parameter (Request)
        var parameters = method.ParameterList.Parameters;
        var newParameters = parameters;

        if (parameters.Count == 0)
            newParameters = SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                             .WithType(SyntaxFactory.ParseTypeName("MyRequest"))
            });

        // 2. Ensure return type is LBTask<TResponse>
        var returnType = method.ReturnType;
        var newReturnType = returnType;

        // Try to infer response type from existing return type if it's generic, else use MyResponse placeholder
        var responseTypeStr = "MyResponse";
        if (returnType is GenericNameSyntax gns && gns.TypeArgumentList.Arguments.Count > 0)
            responseTypeStr = gns.TypeArgumentList.Arguments[0].ToString();

        newReturnType = SyntaxFactory.ParseTypeName($"LBTask<{responseTypeStr}>");

        var newMethod = method
                        .WithReturnType(newReturnType)
                        .WithParameterList(method.ParameterList.WithParameters(newParameters))
                        .WithModifiers(method.Modifiers.Any(SyntaxKind.AsyncKeyword)
                            ? method.Modifiers
                            : method.Modifiers.Add(
                                SyntaxFactory.Token(SyntaxKind.AsyncKeyword))); // Call handlers often want to be async

        var newRoot = root.ReplaceNode(method, newMethod.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }
}