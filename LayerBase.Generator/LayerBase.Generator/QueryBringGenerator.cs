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

/// <summary>
/// [Query] + [Bring] 属性源生成器。
/// 
/// 源生成器：在编译期读取用户代码结构，并额外生成 .g.cs 文件。
/// 这里负责把用户写的：
///     [Query]
///     private void OnMove(ref Position position) { ... }
/// 
/// 转换成类似：
///     public void Move()
///     {
///         var job = new __MoveJob(this);
///         this.Query&lt;Position&gt;().ForEach(ref job);
///     }
/// 
/// 这样外部只需要调用 Move()，内部就能自动走 Query + ForEach 链路。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class QueryBringGenerator : IIncrementalGenerator
{
    private const string QueryAttributeName = "LayerBase.ECS.QueryAttribute";
    private const string BringAttributeName = "LayerBase.ECS.BringAttribute";
    private const string EntryPointAttributeName = "LayerBase.ECS.EntryPointAttribute";
    private const string ProjectResultMetadataName = "LayerBase.ECS.ProjectResult";
    private const string EntityMetadataName = "Arch.Core.Entity";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // SyntaxProvider：Roslyn 增量生成器用于筛选语法节点的入口。
        // ForAttributeWithMetadataName 会先筛选带有指定 Attribute 的语法节点，
        // 再把命中的节点交给 ExtractQueryMethodInfo 做语义分析。
        var queryMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                QueryAttributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => ExtractQueryMethodInfo(ctx))
            .Where(static method => method is not null)
            .Select(static (method, _) => method!);

        // RegisterSourceOutput：注册最终源码输出逻辑。
        // Collect 会把本轮编译收集到的所有 Query 方法合并成一个数组，
        // 这样 Execute 可以按类型统一生成 .g.cs 文件。
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

        // 当前版本只处理 class 中的方法。
        // 如果后续要支持 struct / record，可以在这里扩展 Parent 判断。
        if (ctx.TargetNode.Parent is not ClassDeclarationSyntax classDecl)
        {
            return null;
        }

        // partial：分部类型。
        // 源生成器只能给已有类型补充分部代码，因此用户类必须声明 partial。
        if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return null;
        }

        // 泛型方法会让生成的 Job 参数映射复杂化。
        // 当前先禁止，后续需要时可以记录类型参数并同步生成。
        if (methodSymbol.IsGenericMethod)
        {
            return null;
        }

        var bringAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(static attr => IsAttributeOfMetadataName(attr, BringAttributeName));

        ImmutableArray<ITypeSymbol> bringEventTypes = ImmutableArray<ITypeSymbol>.Empty;

        if (bringAttribute != null)
        {
            // 支持形如 [Bring(typeof(A), typeof(B))] 或类似 params Type[] 的构造参数。
            if (bringAttribute.ConstructorArguments.Length > 0
                && bringAttribute.ConstructorArguments[0].Values.Length > 0)
            {
                bringEventTypes = bringAttribute.ConstructorArguments[0].Values
                    .Where(static value => value.Value is ITypeSymbol)
                    .Select(static value => (ITypeSymbol)value.Value!)
                    .ToImmutableArray();
            }
            // 支持形如 [Bring<A, B>] 这类泛型 Attribute。
            else if (bringAttribute.AttributeClass?.TypeArguments.Length > 0)
            {
                bringEventTypes = bringAttribute.AttributeClass.TypeArguments
                    .ToImmutableArray();
            }
        }

        bool hasBring = bringEventTypes.Length > 0;
        bool returnsVoid = methodSymbol.ReturnsVoid;
        bool returnsProjectResult = IsMetadataType(methodSymbol.ReturnType, ProjectResultMetadataName);

        // Bring 分支需要返回 ProjectResult，
        // 因为 Bring 事件通常需要决定是否继续投递、是否消费、是否短路。
        if (hasBring && !returnsProjectResult)
        {
            return null;
        }

        // 普通 Query 分支只负责遍历组件并执行用户逻辑，
        // 当前约定用户方法必须返回 void。
        if (!hasBring && !returnsVoid)
        {
            return null;
        }

        string? entryPointName = ExtractEntryPointName(methodSymbol);
        if (string.IsNullOrWhiteSpace(entryPointName))
        {
            return null;
        }

        var parameters = methodSymbol.Parameters;

        var componentTypes = new List<ITypeSymbol>();
        var componentRefKinds = new List<RefKind>();
        var userParameters = new List<QueryUserParameterInfo>();

        int entityCount = 0;
        int bringEventCount = 0;
        bool bringTailStarted = false;

        foreach (var param in parameters)
        {
            // Entity：Arch ECS 的实体标识。
            // 它不是组件数据本身，而是当前被遍历实体的句柄。
            if (IsMetadataType(param.Type, EntityMetadataName))
            {
                // Entity 只能出现一次。
                if (entityCount > 0)
                {
                    return null;
                }

                // Bring 事件参数要求位于方法末尾。
                // 因此一旦 Bring 参数开始出现，后面不能再出现 Entity 或组件参数。
                if (bringTailStarted)
                {
                    return null;
                }

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.Entity,
                    Index = -1,
                    RefKind = RefKind.None
                });

                entityCount++;
                continue;
            }

            // Bring 事件参数必须按 BringAttribute 中声明的顺序出现在方法末尾。
            // 例如 [Bring<DamageEvent, HealEvent>] 对应：
            //     ref DamageEvent damage,
            //     ref HealEvent heal
            if (bringEventCount < bringEventTypes.Length
                && SymbolEqualityComparer.Default.Equals(bringEventTypes[bringEventCount], param.Type))
            {
                if (param.RefKind != RefKind.Ref)
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

            // 如果 Bring 参数已经开始，后面不能再追加普通组件参数。
            // 这样能保证 Bring 事件参数确实位于方法参数末尾。
            if (bringTailStarted)
            {
                return null;
            }

            // 组件参数只允许 ref 或 in。
            // ref：允许读写组件。
            // in：只读传入，避免复制大结构体。
            if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.In)
            {
                int componentIndex = componentTypes.Count;

                componentTypes.Add(param.Type);
                componentRefKinds.Add(param.RefKind);

                userParameters.Add(new QueryUserParameterInfo
                {
                    Kind = QueryUserParameterKind.Component,
                    Index = componentIndex,
                    RefKind = param.RefKind
                });

                continue;
            }

            return null;
        }

        // BringAttribute 声明了几个事件，方法末尾就必须接收几个事件。
        if (bringEventCount != bringEventTypes.Length)
        {
            return null;
        }

        // 当前版本先不生成 0 组件 Query。
        // 原因是 IQueryJob<T...> 在没有组件泛型参数时容易生成 IQueryJob<> 这类非法代码。
        // 如果你的框架后续提供 IQueryJob 或 IQueryJobEntityOnly，可以在这里放开。
        if (componentTypes.Count == 0)
        {
            return null;
        }

        return new QueryMethodInfo
        {
            MethodSymbol = methodSymbol,
            ClassDeclaration = classDecl,
            EntryPointName = entryPointName,
            ComponentTypes = componentTypes.ToImmutableArray(),
            ComponentRefKinds = componentRefKinds.ToImmutableArray(),
            BringEventTypes = bringEventTypes,
            UserParameters = userParameters.ToImmutableArray(),
            HasEntity = entityCount > 0,
            ReturnsProjectResult = returnsProjectResult
        };
    }

    private static string? ExtractEntryPointName(IMethodSymbol methodSymbol)
    {
        var entryPointAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(static attr => IsAttributeOfMetadataName(attr, EntryPointAttributeName));

        if (entryPointAttribute != null)
        {
            // [EntryPoint("Move")] 允许用户手动指定生成入口名。
            if (entryPointAttribute.ConstructorArguments.Length == 0)
            {
                return null;
            }

            return entryPointAttribute.ConstructorArguments[0].Value as string;
        }

        // 默认约定：OnXxx -> Xxx。
        // 例如 OnMove 自动生成 public void Move()。
        string methodName = methodSymbol.Name;
        if (!methodName.StartsWith("On", StringComparison.Ordinal))
        {
            return null;
        }

        return methodName.Substring(2);
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<QueryMethodInfo> methods)
    {
        if (methods.IsDefaultOrEmpty)
        {
            return;
        }

        // 按 ClassDeclarationSyntax 分组。
        // 同一个 partial class 里的多个 [Query] 方法会合并到同一个 .g.cs 文件。
        var grouped = methods.GroupBy(static method => method.ClassDeclaration);

        foreach (var group in grouped)
        {
            var firstMethod = group.First();
            var classSymbol = firstMethod.MethodSymbol.ContainingType;

            if (classSymbol == null)
            {
                continue;
            }

            string source = GenerateClassSource(classSymbol, group.ToList());

            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            context.AddSource(
                $"{classSymbol.Name}_QueryBring.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateClassSource(
        INamedTypeSymbol classSymbol,
        List<QueryMethodInfo> methods)
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

        string classDeclaration = BuildPartialClassDeclaration(classSymbol);

        sb.AppendLine($"    {classDeclaration}");
        sb.AppendLine("    {");

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
        bool hasBring = method.BringEventTypes.Length > 0;

        // 生成外部入口方法。
        // 例如用户写 OnMove，默认生成 Move。
        sb.AppendLine($"        public void {entryPoint}()");
        sb.AppendLine("        {");

        if (hasBring)
        {
            GenerateBringInvocation(sb, method);
        }
        else
        {
            GenerateQueryInvocation(sb, method);
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        // Job 结构体必须生成在方法外部、类型内部。
        // 原代码把 Job 生成逻辑放在入口方法内部，会导致生成非法 C#。
        GenerateJobStruct(sb, method);
        sb.AppendLine();
    }

    private static void GenerateQueryInvocation(StringBuilder sb, QueryMethodInfo method)
    {
        string compGeneric = BuildComponentGenericArguments(method);

        // job 是查询执行器。
        // ForEach 会把每个匹配实体和组件传给 job.Execute。
        sb.AppendLine($"            var job = new __{method.EntryPointName}Job(this);");
        sb.AppendLine();

        sb.AppendLine($"            this.Query<{compGeneric}>()");
        sb.AppendLine("                .ForEach(ref job);");
    }

    private static void GenerateBringInvocation(StringBuilder sb, QueryMethodInfo method)
    {
        string compGeneric = BuildComponentGenericArguments(method);
        string eventGeneric = BuildEventGenericArguments(method);

        // Bring：在 Query 组件集合的基础上，把事件数据带入遍历流程。
        // 常见用途是 Query 到目标实体后，对目标实体执行事件投递或事件处理。
        sb.AppendLine($"            var job = new __{method.EntryPointName}Job(this);");
        sb.AppendLine();

        sb.AppendLine($"            this.Query<{compGeneric}>()");
        sb.AppendLine($"                .Bring<{eventGeneric}>()");
        sb.AppendLine("                .ForEach(ref job);");
    }

    private static void GenerateJobStruct(StringBuilder sb, QueryMethodInfo method)
    {
        bool hasBring = method.BringEventTypes.Length > 0;

        string jobGeneric = BuildJobGenericArguments(method);
        string selfTypeName = GetTypeDisplayName(method.MethodSymbol.ContainingType);
        string methodName = method.MethodSymbol.Name;

        // IQueryJob<T...>：Query ForEach 需要的执行接口。
        // 这里的 T... 由组件类型和 Bring 事件类型共同组成。
        sb.AppendLine($"        private readonly struct __{method.EntryPointName}Job : IQueryJob<{jobGeneric}>");
        sb.AppendLine("        {");

        // _self 保存当前业务类实例。
        // Job 是独立 struct，不能直接访问外层 this，所以要显式保存。
        sb.AppendLine($"            private readonly {selfTypeName} _self;");
        sb.AppendLine();

        // self 参数：入口方法所在对象实例。
        // 例如 Move() 中 new __MoveJob(this)，这个 this 就会传到这里。
        sb.AppendLine($"            public __{method.EntryPointName}Job({selfTypeName} self)");
        sb.AppendLine("            {");
        sb.AppendLine("                _self = self;");
        sb.AppendLine("            }");
        sb.AppendLine();

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
            // Bring 方法必须返回 ProjectResult。
            // 因此生成代码也要 return 用户方法结果。
            sb.AppendLine($"                return _self.{methodName}({argStr});");
        }
        else
        {
            // 普通 Query 方法返回 void。
            // 因此这里只调用用户方法，不生成 return。
            sb.AppendLine($"                _self.{methodName}({argStr});");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }

    private static List<string> BuildExecuteParameters(QueryMethodInfo method)
    {
        var parameters = new List<string>
        {
            // Entity 参数固定由 Query Job 接口提供。
            // 即使用户方法不声明 Entity，Execute 也可以接收它，只是不转发给用户方法。
            "Entity entity"
        };

        for (int i = 0; i < method.ComponentTypes.Length; i++)
        {
            string refKind = method.ComponentRefKinds[i] == RefKind.Ref ? "ref" : "in";
            string typeName = GetTypeDisplayName(method.ComponentTypes[i]);

            // c0、c1、c2 是生成代码内部使用的组件变量名。
            // 它们会按用户方法中组件参数的出现顺序排列。
            parameters.Add($"{refKind} {typeName} c{i}");
        }

        for (int i = 0; i < method.BringEventTypes.Length; i++)
        {
            string typeName = GetTypeDisplayName(method.BringEventTypes[i]);

            // Bring 事件统一用 ref 传入，
            // 因为事件可能需要被处理器修改状态。
            parameters.Add($"ref {typeName} e{i}");
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

        return string.Join(" ", parts);
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

    private static string GetTypeDisplayName(ITypeSymbol type)
    {
        // FullyQualifiedFormat 会生成 global::Namespace.TypeName。
        // 这样生成代码不容易受到 using 缺失或同名类型冲突影响。
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
        string expectedShortName = lastDot >= 0
            ? metadataName.Substring(lastDot + 1)
            : metadataName;

        // 泛型 Attribute 的 MetadataName 通常是 BringAttribute`1、BringAttribute`2。
        // 这里把 BringAttribute`1 视作 LayerBase.ECS.BringAttribute。
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

        public ImmutableArray<ITypeSymbol> ComponentTypes { get; set; }

        public ImmutableArray<RefKind> ComponentRefKinds { get; set; }

        public ImmutableArray<ITypeSymbol> BringEventTypes { get; set; }

        public ImmutableArray<QueryUserParameterInfo> UserParameters { get; set; }

        public bool HasEntity { get; set; }

        public bool ReturnsProjectResult { get; set; }
    }

    private sealed class QueryUserParameterInfo
    {
        public QueryUserParameterKind Kind { get; set; }

        public int Index { get; set; }

        public RefKind RefKind { get; set; }
    }

    private enum QueryUserParameterKind
    {
        Entity,
        Component,
        BringEvent
    }
}