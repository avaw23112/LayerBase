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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SubscribeSignatureFixer))]
[Shared]
public class SubscribeSignatureFixer : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LBGS001");

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
                "Fix [Subscribe] signature",
                c => FixSignatureAsync(context.Document, methodDeclaration, c),
                nameof(SubscribeSignatureFixer)),
            diagnostic);
    }

    private async Task<Document> FixSignatureAsync(Document          document, MethodDeclarationSyntax method,
                                                   CancellationToken cancellationToken)
    {
        var attr = method.AttributeLists.SelectMany(al => al.Attributes)
                         .FirstOrDefault(a => a.Name.ToString().StartsWith("Subscribe"));
        if (attr == null) return document;

        var attrName = attr.Name.ToString();
        var isAsync = attrName.Contains("Async");
        var isNotify = attrName.Contains("Notify") || attrName.Contains("NotifySafe");

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var parameters = method.ParameterList.Parameters;
        if (parameters.Count == 0) return document;

        var param = parameters[0];
        var newParam = param;

        // Fix 'in' modifier for non-async
        if (!isAsync)
        {
            if (!param.Modifiers.Any(SyntaxKind.InKeyword))
                newParam = param.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)));
        }
        else
        {
            // For async, remove modifiers if any
            newParam = param.WithModifiers(SyntaxFactory.TokenList());
        }

        var newMethod =
            method.WithParameterList(
                method.ParameterList.WithParameters(SyntaxFactory.SeparatedList(new[] { newParam })));

        // Fix modifiers: Add 'async' if it's an Async subscriber and missing
        if (isAsync && !newMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
            newMethod = newMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        // Fix return type
        if (isAsync)
        {
            var eventType = param.Type?.ToString() ?? "object";
            newMethod = newMethod.WithReturnType(SyntaxFactory.ParseTypeName("LBTask"));
        }
        else if (isNotify)
        {
            newMethod = newMethod.WithReturnType(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)));
        }
        else
        {
            newMethod = newMethod.WithReturnType(SyntaxFactory.ParseTypeName("EventHandledState"));
        }

        var newRoot = root.ReplaceNode(method, newMethod.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }
}

