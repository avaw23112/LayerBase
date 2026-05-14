using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ActorBehaviourGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProvider = context.SyntaxProvider.CreateSyntaxProvider(
                                       static (node, _) => node is ClassDeclarationSyntax classDeclaration &&
                                                           MightContainActorBehaviour(classDeclaration),
                                       static (ctx, _) => GetClassCandidate(ctx))
                                   .Where(static candidate => candidate is not null)!;

        context.RegisterSourceOutput(classProvider.Collect(), static (spc, candidates) => Generate(spc, candidates));
    }

    private static bool MightContainActorBehaviour(ClassDeclarationSyntax classDeclaration)
    {
        foreach (MemberDeclarationSyntax member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && method.AttributeLists.Count > 0)
            {
                return true;
            }
        }

        return classDeclaration.BaseList != null;
    }

    private static ClassCandidate? GetClassCandidate(GeneratorSyntaxContext context)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var methods = new List<MethodCandidate>();
        var callMethods = new List<CallMethodCandidate>();
        foreach (MemberDeclarationSyntax member in declaration.Members)
        {
            if (member is not MethodDeclarationSyntax methodDeclaration)
            {
                continue;
            }

            if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (!HasActorBehaviourAttribute(methodSymbol))
            {
                if (!HasActorCallBehaviourAttribute(methodSymbol))
                {
                    continue;
                }

                callMethods.Add(new CallMethodCandidate(
                    MethodName: methodSymbol.Name,
                    MethodDisplay: methodSymbol.ToDisplayString(),
                    MethodSymbol: methodSymbol,
                    Location: methodSymbol.Locations.FirstOrDefault()));
                continue;
            }

            methods.Add(new MethodCandidate(
                MethodName: methodSymbol.Name,
                MethodDisplay: methodSymbol.ToDisplayString(),
                EventType: methodSymbol.Parameters.Length == 1 ? methodSymbol.Parameters[0].Type : null,
                MethodSymbol: methodSymbol,
                Location: methodSymbol.Locations.FirstOrDefault()));
        }

        bool manuallyImplementsGeneratedMeta = ImplementsInterface(classSymbol, "LayerBase.Actor.IGeneratedActorMeta");
        bool hasTagOrGroupMetadata = HasTagOrGroupMetadata(classSymbol);
        if (methods.Count == 0 && callMethods.Count == 0 && !manuallyImplementsGeneratedMeta && !hasTagOrGroupMetadata)
        {
            return null;
        }

        return new ClassCandidate(
            ClassSymbol: classSymbol,
            Declaration: declaration,
            Methods: methods.ToImmutableArray(),
            CallMethods: callMethods.ToImmutableArray(),
            ManuallyImplementsGeneratedMeta: manuallyImplementsGeneratedMeta,
            HasTagOrGroupMetadata: hasTagOrGroupMetadata);
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<ClassCandidate?> candidates)
    {
        foreach (IGrouping<string, ClassCandidate> group in candidates
                                                            .Where(static candidate => candidate is not null)
                                                            .Select(static candidate => candidate!)
                                                            .GroupBy(static candidate =>
                                                                candidate.ClassSymbol.ToDisplayString(
                                                                    SymbolDisplayFormat.FullyQualifiedFormat)))
        {
            GenerateClass(context, group.ToImmutableArray());
        }
    }

    private static void GenerateClass(SourceProductionContext context, ImmutableArray<ClassCandidate> candidates)
    {
        INamedTypeSymbol classSymbol = candidates[0].ClassSymbol;
        ImmutableArray<MethodCandidate> methods = candidates
                                                  .SelectMany(static candidate => candidate.Methods)
                                                  .GroupBy(static method => method.MethodDisplay)
                                                  .Select(static group => group.First())
                                                  .ToImmutableArray();

        ImmutableArray<CallMethodCandidate> callMethods = candidates
                                                          .SelectMany(static candidate => candidate.CallMethods)
                                                          .GroupBy(static method => method.MethodDisplay)
                                                          .Select(static group => group.First())
                                                          .ToImmutableArray();

        bool hasTagOrGroupMetadata = candidates.Any(static candidate => candidate.HasTagOrGroupMetadata);
        if (methods.Length == 0 && callMethods.Length == 0 && !hasTagOrGroupMetadata)
        {
            return;
        }

        List<Diagnostic> diagnostics = CollectDiagnostics(classSymbol, candidates, methods, callMethods);
        foreach (Diagnostic diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (diagnostics.Count > 0)
        {
            return;
        }

        string source = GenerateSource(classSymbol, methods, callMethods);
        context.AddSource(GetHintName(classSymbol), SourceText.From(source, Encoding.UTF8));
    }

    private static List<Diagnostic> CollectDiagnostics(
        INamedTypeSymbol                    classSymbol,
        ImmutableArray<ClassCandidate>      candidates,
        ImmutableArray<MethodCandidate>     methods,
        ImmutableArray<CallMethodCandidate> callMethods)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (ClassCandidate candidate in candidates)
        {
            if (!candidate.Declaration.Modifiers.Any(static modifier =>
                    modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.ClassMustBePartial,
                    candidate.Declaration.Identifier.GetLocation(),
                    classSymbol.Name));
            }
        }

        if (!ImplementsInterface(classSymbol, "LayerBase.Actor.IActor"))
        {
            diagnostics.Add(Diagnostic.Create(
                ActorBehaviourDiagnostics.ClassMustImplementActor,
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));
        }

        if (candidates.Any(static candidate => candidate.ManuallyImplementsGeneratedMeta))
        {
            diagnostics.Add(Diagnostic.Create(
                ActorBehaviourDiagnostics.ManualGeneratedMetaImplementation,
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));
        }

        var seenEventTypes = new Dictionary<ITypeSymbol, MethodCandidate>(SymbolEqualityComparer.Default);
        foreach (MethodCandidate method in methods)
        {
            IMethodSymbol methodSymbol = method.MethodSymbol;

            if (methodSymbol.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.MethodMustBeInstance,
                    method.Location,
                    method.MethodName));
                continue;
            }

            if (methodSymbol.ReturnsVoid == false)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.MethodMustReturnVoid,
                    method.Location,
                    method.MethodName));
            }

            if (methodSymbol.Parameters.Length != 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.MethodMustHaveSingleParameter,
                    method.Location,
                    method.MethodName));
                continue;
            }

            IParameterSymbol parameter = methodSymbol.Parameters[0];
            if (parameter.RefKind != RefKind.In)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.ParameterMustBeInStructEvent,
                    parameter.Locations.FirstOrDefault() ?? method.Location,
                    method.MethodName));
                continue;
            }

            if (parameter.Type.IsValueType == false)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.EventTypeMustBeStruct,
                    parameter.Locations.FirstOrDefault() ?? method.Location,
                    method.MethodName,
                    parameter.Type.ToDisplayString()));
                continue;
            }

            if (seenEventTypes.ContainsKey(parameter.Type))
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.DuplicateEventType,
                    parameter.Locations.FirstOrDefault() ?? method.Location,
                    classSymbol.Name,
                    parameter.Type.ToDisplayString()));
                continue;
            }

            seenEventTypes.Add(parameter.Type, method);
        }

        var seenCallRoutes = new Dictionary<string, CallMethodCandidate>(StringComparer.Ordinal);
        foreach (CallMethodCandidate method in callMethods)
        {
            IMethodSymbol methodSymbol = method.MethodSymbol;

            if (methodSymbol.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.MethodMustBeInstance,
                    method.Location,
                    method.MethodName));
                continue;
            }

            if (methodSymbol.IsGenericMethod)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallMethodCannotBeGeneric,
                    method.Location,
                    method.MethodName));
                continue;
            }

            if (!TryGetLBTaskResultType(methodSymbol.ReturnType, out ITypeSymbol? responseType))
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallMethodMustReturnLBTask,
                    method.Location,
                    method.MethodName));
                continue;
            }

            if (responseType!.IsValueType == false)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallResponseTypeMustBeStruct,
                    method.Location,
                    method.MethodName,
                    responseType.ToDisplayString()));
                continue;
            }

            if (methodSymbol.Parameters.Length != 2)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallMethodMustHaveRequestAndCancellationToken,
                    method.Location,
                    method.MethodName));
                continue;
            }

            IParameterSymbol requestParameter = methodSymbol.Parameters[0];
            if (requestParameter.RefKind != RefKind.In)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallMethodMustHaveRequestAndCancellationToken,
                    requestParameter.Locations.FirstOrDefault() ?? method.Location,
                    method.MethodName));
                continue;
            }

            if (requestParameter.Type.IsValueType == false)
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallRequestTypeMustBeStruct,
                    requestParameter.Locations.FirstOrDefault() ?? method.Location,
                    method.MethodName,
                    requestParameter.Type.ToDisplayString()));
                continue;
            }

            IParameterSymbol cancellationTokenParameter = methodSymbol.Parameters[1];
            if (cancellationTokenParameter.RefKind != RefKind.None
                || cancellationTokenParameter.Type.ToDisplayString() != "System.Threading.CancellationToken")
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.CallMethodMustHaveRequestAndCancellationToken,
                    cancellationTokenParameter.Locations.FirstOrDefault() ?? method.Location,
                    method.MethodName));
                continue;
            }

            string routeKey = requestParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                              + "->"
                              + responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (seenCallRoutes.ContainsKey(routeKey))
            {
                diagnostics.Add(Diagnostic.Create(
                    ActorBehaviourDiagnostics.DuplicateCallRoute,
                    method.Location,
                    classSymbol.Name,
                    requestParameter.Type.ToDisplayString(),
                    responseType.ToDisplayString()));
                continue;
            }

            seenCallRoutes.Add(routeKey, method);
        }

        return diagnostics;
    }

    private static string GenerateSource(
        INamedTypeSymbol                    classSymbol,
        ImmutableArray<MethodCandidate>     methods,
        ImmutableArray<CallMethodCandidate> callMethods)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");

        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append("namespace ");
            builder.AppendLine(classSymbol.ContainingNamespace.ToDisplayString());
            builder.AppendLine("{");
        }

        AppendContainingTypesStart(builder, classSymbol.ContainingType, 1);

        string indent = GetIndent(classSymbol.ContainingType, 1);
        builder.Append(indent);
        builder.Append(GetAccessibility(classSymbol.DeclaredAccessibility));
        builder.Append(" partial class ");
        builder.Append(classSymbol.Name);
        builder.Append(GetTypeParameterList(classSymbol));
        builder.AppendLine(" : global::LayerBase.Actor.IGeneratedActorMeta");
        AppendConstraintClauses(builder, classSymbol, indent);
        builder.Append(indent);
        builder.AppendLine("{");

        string memberIndent = indent + "    ";
        builder.Append(memberIndent);
        builder.AppendLine("public global::LayerBase.Actor.ActorContext Context { get; private set; }");
        builder.AppendLine();
        builder.Append(memberIndent);
        builder.AppendLine("global::LayerBase.Actor.ActorId global::LayerBase.Actor.IGeneratedActorMeta.GetId()");
        builder.Append(memberIndent);
        builder.AppendLine("{");
        builder.Append(memberIndent);
        builder.AppendLine("    return Context.ActorId;");
        builder.Append(memberIndent);
        builder.AppendLine("}");
        builder.AppendLine();

        builder.Append(memberIndent);
        builder.AppendLine(
            "void global::LayerBase.Actor.IGeneratedActorMeta.ActorInit(global::LayerBase.Actor.ActorContext context)");
        builder.Append(memberIndent);
        builder.AppendLine("{");
        builder.Append(memberIndent);
        builder.AppendLine("    Context = context;");
        builder.Append(memberIndent);
        builder.AppendLine("}");
        builder.AppendLine();

        builder.Append(memberIndent);
        builder.AppendLine(
            "void global::LayerBase.Actor.IGeneratedActorMeta.__BuildActorMeta(global::LayerBase.Actor.ActorTypeMetaBuilder builder)");
        builder.Append(memberIndent);
        builder.AppendLine("{");

        string actorTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        foreach (MethodCandidate method in methods.OrderBy(static method => method.MethodName, StringComparer.Ordinal))
        {
            string eventTypeName = method.EventType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append(memberIndent);
            builder.Append("    builder.AddBehaviour<");
            builder.Append(actorTypeName);
            builder.Append(", ");
            builder.Append(eventTypeName);
            builder.AppendLine(">(");
            builder.Append(memberIndent);
            builder.Append("        static (");
            builder.Append(actorTypeName);
            builder.AppendLine(" actor) => actor.");
            builder.Append(method.MethodName);
            builder.AppendLine(");");
        }

        foreach (CallMethodCandidate method in callMethods.OrderBy(static method => method.MethodName,
                     StringComparer.Ordinal))
        {
            IMethodSymbol methodSymbol = method.MethodSymbol;
            ITypeSymbol requestType = methodSymbol.Parameters[0].Type;
            ITypeSymbol responseType = ((INamedTypeSymbol)methodSymbol.ReturnType).TypeArguments[0];
            string requestTypeName = requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string responseTypeName = responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            builder.Append(memberIndent);
            builder.Append("    builder.AddCallBehaviour<");
            builder.Append(actorTypeName);
            builder.Append(", ");
            builder.Append(requestTypeName);
            builder.Append(", ");
            builder.Append(responseTypeName);
            builder.AppendLine(">(");
            builder.Append(memberIndent);
            builder.Append("        static (");
            builder.Append(actorTypeName);
            builder.Append(" actor, in ");
            builder.Append(requestTypeName);
            builder.Append(" request, global::System.Threading.CancellationToken cancellationToken) =>");
            builder.AppendLine();
            builder.Append(memberIndent);
            builder.AppendLine("        {");
            builder.Append(memberIndent);
            builder.Append("            return actor.");
            builder.Append(method.MethodName);
            builder.Append("(in request, cancellationToken);");
            builder.AppendLine();
            builder.Append(memberIndent);
            builder.AppendLine("        });");
        }

        foreach (INamedTypeSymbol tagType in GetTagTypes(classSymbol))
        {
            builder.Append(memberIndent);
            builder.Append("    builder.AddTag<");
            builder.Append(tagType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            builder.AppendLine(">();");
        }

        foreach (INamedTypeSymbol groupType in GetGroupTypes(classSymbol))
        {
            builder.Append(memberIndent);
            builder.Append("    builder.AddGroup<");
            builder.Append(groupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            builder.AppendLine(">();");
        }

        builder.Append(memberIndent);
        builder.AppendLine("}");
        builder.Append(indent);
        builder.AppendLine("}");

        AppendContainingTypesEnd(builder, classSymbol.ContainingType, 1);

        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static void AppendContainingTypesStart(StringBuilder builder, INamedTypeSymbol? typeSymbol, int level)
    {
        if (typeSymbol == null)
        {
            return;
        }

        AppendContainingTypesStart(builder, typeSymbol.ContainingType, level);
        string indent = new(' ', level * 4);
        builder.Append(indent);
        builder.Append(GetAccessibility(typeSymbol.DeclaredAccessibility));
        builder.Append(" partial class ");
        builder.Append(typeSymbol.Name);
        builder.Append(GetTypeParameterList(typeSymbol));
        builder.AppendLine();
        AppendConstraintClauses(builder, typeSymbol, indent);
        builder.Append(indent);
        builder.AppendLine("{");
    }

    private static void AppendContainingTypesEnd(StringBuilder builder, INamedTypeSymbol? typeSymbol, int level)
    {
        if (typeSymbol == null)
        {
            return;
        }

        AppendContainingTypesEnd(builder, typeSymbol.ContainingType, level);
        string indent = new(' ', level * 4);
        builder.Append(indent);
        builder.AppendLine("}");
    }

    private static void AppendConstraintClauses(StringBuilder builder, INamedTypeSymbol symbol, string indent)
    {
        foreach (ITypeParameterSymbol typeParameter in symbol.TypeParameters)
        {
            string constraint = BuildConstraintClause(typeParameter);
            if (constraint.Length == 0)
            {
                continue;
            }

            builder.Append(indent);
            builder.AppendLine(constraint);
        }
    }

    private static string BuildConstraintClause(ITypeParameterSymbol typeParameter)
    {
        var parts = new List<string>();

        switch (typeParameter.HasReferenceTypeConstraint, typeParameter.ReferenceTypeConstraintNullableAnnotation)
        {
            case (true, NullableAnnotation.Annotated):
                parts.Add("class?");
                break;
            case (true, _):
                parts.Add("class");
                break;
        }

        if (typeParameter.HasUnmanagedTypeConstraint)
        {
            parts.Add("unmanaged");
        }
        else if (typeParameter.HasValueTypeConstraint)
        {
            parts.Add("struct");
        }

        foreach (ITypeSymbol constraintType in typeParameter.ConstraintTypes)
        {
            parts.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (typeParameter.HasNotNullConstraint)
        {
            parts.Add("notnull");
        }

        if (typeParameter.HasConstructorConstraint)
        {
            parts.Add("new()");
        }

        return parts.Count == 0
            ? string.Empty
            : $"where {typeParameter.Name} : {string.Join(", ", parts)}";
    }

    private static string GetTypeParameterList(INamedTypeSymbol symbol)
    {
        if (symbol.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return "<" + string.Join(", ", symbol.TypeParameters.Select(static parameter => parameter.Name)) + ">";
    }

    private static string GetIndent(INamedTypeSymbol? containingType, int namespaceLevel)
    {
        int level = namespaceLevel;
        for (INamedTypeSymbol? current = containingType; current != null; current = current.ContainingType)
        {
            level++;
        }

        return new string(' ', level * 4);
    }

    private static string GetAccessibility(Accessibility accessibility)
    {
        return accessibility switch
               {
                   Accessibility.Public               => "public",
                   Accessibility.Internal             => "internal",
                   Accessibility.Private              => "private",
                   Accessibility.Protected            => "protected",
                   Accessibility.ProtectedAndInternal => "private protected",
                   Accessibility.ProtectedOrInternal  => "protected internal",
                   _                                  => "internal"
               };
    }

    private static bool HasActorBehaviourAttribute(IMethodSymbol methodSymbol)
    {
        foreach (AttributeData attribute in methodSymbol.GetAttributes())
        {
            string? attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName == "LayerBase.Actor.ActorBehaviourAttribute"
                || attributeName == "LayerBase.Actor.ActorBehavioursAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasActorCallBehaviourAttribute(IMethodSymbol methodSymbol)
    {
        foreach (AttributeData attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "LayerBase.Actor.ActorCallBehaviourAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetLBTaskResultType(ITypeSymbol returnType, out ITypeSymbol? resultType)
    {
        if (returnType is INamedTypeSymbol namedType
            && namedType.Arity == 1
            && namedType.Name == "LBTask"
            && namedType.ContainingNamespace.ToDisplayString() == "LayerBase.Async")
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        resultType = null;
        return false;
    }

    private static bool HasTagOrGroupMetadata(INamedTypeSymbol classSymbol)
    {
        return GetTagTypes(classSymbol).Length > 0
               || GetGroupTypes(classSymbol).Length > 0;
    }

    private static ImmutableArray<INamedTypeSymbol> GetTagTypes(INamedTypeSymbol classSymbol)
    {
        return GetGenericAttributeTypeArguments(classSymbol, "TagAttribute");
    }

    private static ImmutableArray<INamedTypeSymbol> GetGroupTypes(INamedTypeSymbol classSymbol)
    {
        return GetGenericAttributeTypeArguments(classSymbol, "GroupAttribute");
    }

    private static ImmutableArray<INamedTypeSymbol> GetGenericAttributeTypeArguments(
        INamedTypeSymbol classSymbol,
        string           attributeName)
    {
        return classSymbol.GetAttributes()
                          .Where(attribute => IsLayerBaseActorGenericAttribute(attribute, attributeName))
                          .Select(attribute => attribute.AttributeClass!.TypeArguments[0])
                          .OfType<INamedTypeSymbol>()
                          .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                          .OrderBy(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                              StringComparer.Ordinal)
                          .ToImmutableArray();
    }

    private static bool IsLayerBaseActorGenericAttribute(AttributeData attribute, string attributeName)
    {
        if (attribute.AttributeClass is not INamedTypeSymbol attributeClass)
        {
            return false;
        }

        return attributeClass.Name == attributeName
               && attributeClass.TypeArguments.Length == 1
               && attributeClass.ContainingNamespace.ToDisplayString() == "LayerBase.Actor";
    }

    private static bool ImplementsInterface(INamedTypeSymbol symbol, string interfaceName)
    {
        return symbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.ToDisplayString() == interfaceName);
    }

    private static string GetHintName(INamedTypeSymbol symbol)
    {
        string name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", string.Empty)
                            .Replace('<', '_')
                            .Replace('>', '_')
                            .Replace('.', '_')
                            .Replace(':', '_');

        return $"{name}.ActorBehaviour.g.cs";
    }

    private sealed record ClassCandidate(
        INamedTypeSymbol                    ClassSymbol,
        ClassDeclarationSyntax              Declaration,
        ImmutableArray<MethodCandidate>     Methods,
        ImmutableArray<CallMethodCandidate> CallMethods,
        bool                                ManuallyImplementsGeneratedMeta,
        bool                                HasTagOrGroupMetadata);

    private sealed record MethodCandidate(
        string        MethodName,
        string        MethodDisplay,
        ITypeSymbol?  EventType,
        IMethodSymbol MethodSymbol,
        Location?     Location);

    private sealed record CallMethodCandidate(
        string        MethodName,
        string        MethodDisplay,
        IMethodSymbol MethodSymbol,
        Location?     Location);
}