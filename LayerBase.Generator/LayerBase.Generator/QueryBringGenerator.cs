using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class QueryBringGenerator : IIncrementalGenerator
{
    private const string QueryAttributeName = "LayerBase.ECS.QueryAttribute";
    private const string BringAttributeName = "LayerBase.ECS.BringAttribute";
    private const string EntryPointAttributeName = "LayerBase.ECS.EntryPointAttribute";
    private const string ProjectResultMetadataName = "LayerBase.ECS.ProjectResult";
    private const string EntityMetadataName = "Arch.Core.Entity";
    private const string IComponentMetadataName = "LayerBase.Core.IComponent";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var queryMethods = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      QueryAttributeName,
                                      static (node, _) => node is MethodDeclarationSyntax,
                                      static (ctx, _) => ExtractQueryMethodInfo(ctx))
                                  .Where(static method => method is not null)
                                  .Select(static (method, _) => method!);

        context.RegisterSourceOutput(
            queryMethods.Collect(),
            static (spc, methods) => Execute(spc, methods));
    }

    private static QueryMethodInfo? ExtractQueryMethodInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (ctx.TargetNode.Parent is not ClassDeclarationSyntax classDecl)
        {
            return null;
        }

        if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword) ||
            methodSymbol.IsGenericMethod ||
            !methodSymbol.IsStatic)
        {
            return null;
        }

        ImmutableArray<ITypeSymbol> bringEventTypes = ExtractBringEventTypes(methodSymbol);
        bool hasBring = bringEventTypes.Length > 0;
        bool returnsProjectResult = IsMetadataType(methodSymbol.ReturnType, ProjectResultMetadataName);

        if (hasBring)
        {
            if (!returnsProjectResult)
            {
                return null;
            }
        }
        else if (!methodSymbol.ReturnsVoid)
        {
            return null;
        }

        string? entryPointName = ExtractEntryPointName(methodSymbol);
        if (string.IsNullOrWhiteSpace(entryPointName))
        {
            return null;
        }

        var inputParameters = new List<QueryInputParameterInfo>();
        var componentTypes = new List<ITypeSymbol>();
        var componentRefKinds = new List<RefKind>();
        var userParameters = new List<QueryUserParameterInfo>();

        int entityCount = 0;
        int bringEventCount = 0;
        bool componentStarted = false;
        bool bringTailStarted = false;

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (IsMetadataType(parameter.Type, EntityMetadataName))
            {
                if (entityCount > 0 || bringTailStarted)
                {
                    return null;
                }

                componentStarted = true;
                entityCount++;

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.Entity,
                    Index = -1,
                    RefKind = RefKind.None
                });

                continue;
            }

            if (bringEventCount < bringEventTypes.Length &&
                SymbolEqualityComparer.Default.Equals(bringEventTypes[bringEventCount], parameter.Type))
            {
                if (parameter.RefKind != RefKind.Ref)
                {
                    return null;
                }

                bringTailStarted = true;

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.BringEvent,
                    Index = bringEventCount,
                    RefKind = RefKind.Ref
                });

                bringEventCount++;
                continue;
            }

            if (bringTailStarted)
            {
                return null;
            }

            if (IsComponentParameter(parameter))
            {
                componentStarted = true;
                int componentIndex = componentTypes.Count;

                componentTypes.Add(parameter.Type);
                componentRefKinds.Add(parameter.RefKind);

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.Component,
                    Index = componentIndex,
                    RefKind = parameter.RefKind
                });

                continue;
            }

            if (IsInputParameter(parameter))
            {
                if (componentStarted)
                {
                    return null;
                }

                int inputIndex = inputParameters.Count;
                inputParameters.Add(new QueryInputParameterInfo
                {
                    Name = parameter.Name,
                    Type = parameter.Type,
                    RefKind = parameter.RefKind,
                    Index = inputIndex
                });

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.Input,
                    Index = inputIndex,
                    RefKind = parameter.RefKind
                });

                continue;
            }

            return null;
        }

        if (bringEventCount != bringEventTypes.Length || componentTypes.Count == 0)
        {
            return null;
        }

        return new QueryMethodInfo
        {
            MethodSymbol = methodSymbol,
            ClassDeclaration = classDecl,
            EntryPointName = entryPointName,
            InputParameters = inputParameters.ToImmutableArray(),
            ComponentTypes = componentTypes.ToImmutableArray(),
            ComponentRefKinds = componentRefKinds.ToImmutableArray(),
            BringEventTypes = bringEventTypes,
            UserParameters = userParameters.ToImmutableArray(),
            HasEntity = entityCount > 0,
            ReturnsProjectResult = returnsProjectResult
        };
    }

    private static ImmutableArray<ITypeSymbol> ExtractBringEventTypes(IMethodSymbol methodSymbol)
    {
        var bringAttribute = methodSymbol.GetAttributes()
                                         .FirstOrDefault(static attr =>
                                             IsAttributeOfMetadataName(attr, BringAttributeName));

        if (bringAttribute == null)
        {
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        if (bringAttribute.ConstructorArguments.Length > 0 &&
            bringAttribute.ConstructorArguments[0].Values.Length > 0)
        {
            return bringAttribute.ConstructorArguments[0].Values
                                 .Where(static value => value.Value is ITypeSymbol)
                                 .Select(static value => (ITypeSymbol)value.Value!)
                                 .ToImmutableArray();
        }

        if (bringAttribute.AttributeClass?.TypeArguments.Length > 0)
        {
            return bringAttribute.AttributeClass.TypeArguments.ToImmutableArray();
        }

        return ImmutableArray<ITypeSymbol>.Empty;
    }

    private static string? ExtractEntryPointName(IMethodSymbol methodSymbol)
    {
        var entryPointAttribute = methodSymbol.GetAttributes()
                                              .FirstOrDefault(static attr =>
                                                  IsAttributeOfMetadataName(attr, EntryPointAttributeName));

        if (entryPointAttribute != null)
        {
            if (entryPointAttribute.ConstructorArguments.Length == 0)
            {
                return null;
            }

            return entryPointAttribute.ConstructorArguments[0].Value as string;
        }

        string methodName = methodSymbol.Name;
        return methodName.StartsWith("On", StringComparison.Ordinal)
            ? methodName.Substring(2)
            : null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<QueryMethodInfo> methods)
    {
        if (methods.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var group in methods.GroupBy(static method => method.ClassDeclaration))
        {
            var classSymbol = group.First().MethodSymbol.ContainingType;
            if (classSymbol == null)
            {
                continue;
            }

            string source = GenerateClassSource(classSymbol, group.ToList());
            if (!string.IsNullOrWhiteSpace(source))
            {
                context.AddSource($"{classSymbol.Name}_QueryBring.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateClassSource(INamedTypeSymbol classSymbol, List<QueryMethodInfo> methods)
    {
        var sb = new StringBuilder();
        string ns = classSymbol.ContainingNamespace.ToDisplayString();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Arch.Core;");
        sb.AppendLine("using LayerBase;");
        sb.AppendLine("using LayerBase.ECS;");
        sb.AppendLine("using LayerBase.ECS.Projection.Flow;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ns) && ns != "<global namespace>")
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    {BuildPartialClassDeclaration(classSymbol)}");
        sb.AppendLine("    {");

        sb.AppendLine("        private global::LayerBase.LayerRuntime __Runtime = null!;");
        foreach (var method in methods)
        {
            sb.AppendLine($"        private int __{method.EntryPointName}QueryId;");
        }

        sb.AppendLine();
        sb.AppendLine("        void global::LayerBase.ECS.IGeneratedEcsQueryRegistrar.RegisterGeneratedEcsQueries(");
        sb.AppendLine("            global::LayerBase.LayerRuntime runtime)");
        sb.AppendLine("        {");
        sb.AppendLine("            __Runtime = runtime;");
        sb.AppendLine();
        foreach (var method in methods)
        {
            string compGeneric = BuildComponentGenericArguments(method);
            sb.AppendLine($"            __{method.EntryPointName}QueryId = runtime.EcsQueryRegistry.GetOrCreate<{compGeneric}>();");
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        foreach (var method in methods)
        {
            GenerateMethodSource(sb, method);
        }

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(ns) && ns != "<global namespace>")
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static void GenerateMethodSource(StringBuilder sb, QueryMethodInfo method)
    {
        string entryPoint = method.EntryPointName!;
        string entryParameters = BuildEntryPointParameterList(method);

        sb.AppendLine($"        public void {entryPoint}({entryParameters})");
        sb.AppendLine("        {");

        if (method.BringEventTypes.Length > 0)
        {
            GenerateBringInvocation(sb, method);
        }
        else
        {
            GenerateQueryInvocation(sb, method);
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        GenerateJobStruct(sb, method);
        sb.AppendLine();
    }

    private static void GenerateQueryInvocation(StringBuilder sb, QueryMethodInfo method)
    {
        string compGeneric = BuildComponentGenericArguments(method);
        string inputArgs = BuildInputArgumentList(method);

        sb.AppendLine($"            var job = new __{method.EntryPointName}Job({inputArgs});");
        sb.AppendLine();
        sb.AppendLine("            if (__Runtime is null)");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new global::System.InvalidOperationException(\"Generated ECS queries are not registered.\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            __Runtime.EcsScheduler");
        sb.AppendLine($"                .SubmitPlainQuery<__{method.EntryPointName}Job, {compGeneric}>(");
        sb.AppendLine($"                    __{method.EntryPointName}QueryId,");
        sb.AppendLine("                    null,");
        sb.AppendLine("                    in job);");
    }

    private static void GenerateBringInvocation(StringBuilder sb, QueryMethodInfo method)
    {
        string compGeneric = BuildComponentGenericArguments(method);
        string eventGeneric = BuildEventGenericArguments(method);
        string inputArgs = BuildInputArgumentList(method);

        sb.AppendLine($"            var job = new __{method.EntryPointName}Job({inputArgs});");
        sb.AppendLine();
        sb.AppendLine("            if (__Runtime is null)");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new global::System.InvalidOperationException(\"Generated ECS queries are not registered.\");");
        sb.AppendLine("            }");
        sb.AppendLine();

        if (method.BringEventTypes.Length == 1)
        {
            sb.AppendLine("            __Runtime.EcsScheduler");
            sb.AppendLine($"                .SubmitBringQuery<{eventGeneric}, __{method.EntryPointName}Job, {compGeneric}>(");
            sb.AppendLine($"                    __{method.EntryPointName}QueryId,");
            sb.AppendLine("                    null,");
            sb.AppendLine("                    in job);");
            return;
        }

        sb.AppendLine("            __Runtime.EcsWorld");
        sb.AppendLine($"                .Query<{compGeneric}>()");
        sb.AppendLine($"                .Bring<{eventGeneric}>()");
        sb.AppendLine("                .ForEach(ref job)");
        sb.AppendLine("                .Batch()");
        sb.AppendLine("                .Post();");
    }

    private static void GenerateJobStruct(StringBuilder sb, QueryMethodInfo method)
    {
        bool hasBring = method.BringEventTypes.Length > 0;
        string jobInterfaceName = BuildJobInterfaceName(method);
        string methodName = method.MethodSymbol.Name;

        sb.AppendLine($"        private readonly struct __{method.EntryPointName}Job : {jobInterfaceName}");
        sb.AppendLine("        {");

        EmitInputFieldsAndConstructor(sb, method);

        string returnType = hasBring ? "ProjectResult" : "void";
        sb.AppendLine($"            public {returnType} Execute(");

        var executeParameters = BuildExecuteParameters(method);
        for (int i = 0; i < executeParameters.Count; i++)
        {
            string comma = i < executeParameters.Count - 1 ? "," : "";
            sb.AppendLine($"                {executeParameters[i]}{comma}");
        }

        sb.AppendLine("            )");
        sb.AppendLine("            {");

        string argStr = BuildUserMethodArgumentList(method);
        if (hasBring)
        {
            sb.AppendLine($"                return {methodName}({argStr});");
        }
        else
        {
            sb.AppendLine($"                {methodName}({argStr});");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }

    private static void EmitInputFieldsAndConstructor(StringBuilder sb, QueryMethodInfo method)
    {
        if (method.InputParameters.Length == 0)
        {
            return;
        }

        foreach (var input in method.InputParameters)
        {
            sb.AppendLine($"            private readonly {GetTypeDisplayName(input.Type)} {GetInputFieldName(input)};");
        }

        sb.AppendLine();
        sb.AppendLine($"            public __{method.EntryPointName}Job({BuildEntryPointParameterList(method)})");
        sb.AppendLine("            {");

        foreach (var input in method.InputParameters)
        {
            sb.AppendLine($"                {GetInputFieldName(input)} = {input.Name};");
        }

        sb.AppendLine("            }");
        sb.AppendLine();
    }

    private static List<string> BuildExecuteParameters(QueryMethodInfo method)
    {
        bool hasBring = method.BringEventTypes.Length > 0;

        var parameters = new List<string>
        {
            "Entity entity"
        };

        for (int i = 0; i < method.ComponentTypes.Length; i++)
        {
            string typeName = GetTypeDisplayName(method.ComponentTypes[i]);
            parameters.Add($"ref {typeName} c{i}");
        }

        for (int i = 0; i < method.BringEventTypes.Length; i++)
        {
            parameters.Add($"ref {GetTypeDisplayName(method.BringEventTypes[i])} e{i}");
        }

        return parameters;
    }

    private static string BuildUserMethodArgumentList(QueryMethodInfo method)
    {
        var args = new List<string>();

        foreach (var userParameter in method.UserParameters)
        {
            switch (userParameter.Kind)
            {
                case QueryUserParameterKind.Input:
                {
                    var input = method.InputParameters[userParameter.Index];
                    string fieldName = GetInputFieldName(input);
                    args.Add(input.RefKind == RefKind.In ? $"in {fieldName}" : fieldName);
                    break;
                }
                case QueryUserParameterKind.Entity:
                    args.Add("entity");
                    break;
                case QueryUserParameterKind.Component:
                {
                    string refKind = userParameter.RefKind == RefKind.Ref ? "ref" : "in";
                    args.Add($"{refKind} c{userParameter.Index}");
                    break;
                }
                case QueryUserParameterKind.BringEvent:
                    args.Add($"ref e{userParameter.Index}");
                    break;
            }
        }

        return string.Join(", ", args);
    }

    private static string BuildEntryPointParameterList(QueryMethodInfo method)
    {
        return string.Join(", ", method.InputParameters.Select(static input =>
        {
            string prefix = input.RefKind == RefKind.In ? "in " : string.Empty;
            return $"{prefix}{GetTypeDisplayName(input.Type)} {input.Name}";
        }));
    }

    private static string BuildInputArgumentList(QueryMethodInfo method)
    {
        return string.Join(", ", method.InputParameters.Select(static input =>
            input.RefKind == RefKind.In ? $"in {input.Name}" : input.Name));
    }

    private static string BuildComponentGenericArguments(QueryMethodInfo method)
    {
        return string.Join(", ", method.ComponentTypes.Select(static type => GetTypeDisplayName(type)));
    }

    private static string BuildEventGenericArguments(QueryMethodInfo method)
    {
        return string.Join(", ", method.BringEventTypes.Select(static type => GetTypeDisplayName(type)));
    }

    private static string BuildJobGenericArguments(QueryMethodInfo method)
    {
        return string.Join(
            ", ",
            method.ComponentTypes
                  .Concat(method.BringEventTypes)
                  .Select(static type => GetTypeDisplayName(type)));
    }

    private static string BuildJobInterfaceName(QueryMethodInfo method)
    {
        string jobGeneric = BuildJobGenericArguments(method);
        if (method.BringEventTypes.Length == 0)
        {
            return $"IQueryJob<{jobGeneric}>";
        }

        return $"IProjectionJob{method.ComponentTypes.Length}x{method.BringEventTypes.Length}<{jobGeneric}>";
    }

    private static string BuildPartialClassDeclaration(INamedTypeSymbol classSymbol)
    {
        var parts = new List<string>();

        string accessibility = classSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "internal"
        };

        parts.Add(accessibility);

        if (classSymbol.IsAbstract && classSymbol.IsSealed)
        {
            parts.Add("static");
        }
        else
        {
            if (classSymbol.IsAbstract)
            {
                parts.Add("abstract");
            }

            if (classSymbol.IsSealed)
            {
                parts.Add("sealed");
            }
        }

        parts.Add("partial");
        parts.Add("class");
        parts.Add(BuildTypeDeclarationName(classSymbol));

        string declaration = string.Join(" ", parts);
        if (!(classSymbol.IsAbstract && classSymbol.IsSealed))
        {
            declaration += " : global::LayerBase.ECS.IGeneratedEcsQueryRegistrar";
        }

        return declaration;
    }

    private static string BuildTypeDeclarationName(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.TypeParameters.Length == 0)
        {
            return classSymbol.Name;
        }

        string typeParameters = string.Join(", ", classSymbol.TypeParameters.Select(static parameter => parameter.Name));
        return $"{classSymbol.Name}<{typeParameters}>";
    }

    private static string GetInputFieldName(QueryInputParameterInfo input)
    {
        return string.IsNullOrWhiteSpace(input.Name) ? $"_input{input.Index}" : "_" + input.Name;
    }

    private static string GetTypeDisplayName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static bool IsComponentParameter(IParameterSymbol parameter)
    {
        return (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.In) &&
               ImplementsInterface(parameter.Type, IComponentMetadataName);
    }

    private static bool IsInputParameter(IParameterSymbol parameter)
    {
        if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
        {
            return false;
        }

        if (!parameter.Type.IsValueType || IsMetadataType(parameter.Type, EntityMetadataName))
        {
            return false;
        }

        return !ImplementsInterface(parameter.Type, IComponentMetadataName);
    }

    private static bool ImplementsInterface(ITypeSymbol type, string interfaceMetadataName)
    {
        return type.AllInterfaces.Any(i => IsMetadataType(i, interfaceMetadataName));
    }

    private static bool IsMetadataType(ITypeSymbol? type, string metadataName)
    {
        if (type == null)
        {
            return false;
        }

        string ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string fullName = string.IsNullOrEmpty(ns)
            ? type.MetadataName
            : $"{ns}.{type.MetadataName}";

        return fullName == metadataName;
    }

    private static bool IsAttributeOfMetadataName(AttributeData attribute, string metadataName)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass == null)
        {
            return false;
        }

        string ns = attributeClass.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string fullMetadataName = string.IsNullOrEmpty(ns)
            ? attributeClass.MetadataName
            : $"{ns}.{attributeClass.MetadataName}";

        if (fullMetadataName == metadataName)
        {
            return true;
        }

        int lastDot = metadataName.LastIndexOf('.');
        string expectedShortName = lastDot >= 0 ? metadataName.Substring(lastDot + 1) : metadataName;

        if (!attributeClass.MetadataName.StartsWith(expectedShortName + "`", StringComparison.Ordinal))
        {
            return false;
        }

        string fullNonGenericName = string.IsNullOrEmpty(ns)
            ? expectedShortName
            : $"{ns}.{expectedShortName}";

        return fullNonGenericName == metadataName;
    }

    private sealed class QueryMethodInfo
    {
        public IMethodSymbol MethodSymbol { get; set; } = null!;

        public ClassDeclarationSyntax ClassDeclaration { get; set; } = null!;

        public string? EntryPointName { get; set; }

        public ImmutableArray<QueryInputParameterInfo> InputParameters { get; set; }

        public ImmutableArray<ITypeSymbol> ComponentTypes { get; set; }

        public ImmutableArray<RefKind> ComponentRefKinds { get; set; }

        public ImmutableArray<ITypeSymbol> BringEventTypes { get; set; }

        public ImmutableArray<QueryUserParameterInfo> UserParameters { get; set; }

        public bool HasEntity { get; set; }

        public bool ReturnsProjectResult { get; set; }
    }

    private sealed class QueryInputParameterInfo
    {
        public string Name { get; set; } = string.Empty;

        public ITypeSymbol Type { get; set; } = null!;

        public RefKind RefKind { get; set; }

        public int Index { get; set; }
    }

    private sealed class QueryUserParameterInfo
    {
        public QueryUserParameterKind Kind { get; set; }

        public int Index { get; set; }

        public RefKind RefKind { get; set; }
    }

    private enum QueryUserParameterKind
    {
        Input,
        Entity,
        Component,
        BringEvent
    }
}
