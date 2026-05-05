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
public sealed class LayerServiceGenerator : IIncrementalGenerator
{
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string MountAttributeName = "LayerBase.DI.MountAttribute";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ILayerContextMetadataName = "LayerBase.DI.ILayerContext";
    private const string LayerMetadataName = "LayerBase.Layers.Layer";
    private const string EventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandler`1";
    private const string EventHandlerAsyncMetadataName = "LayerBase.Core.EventHandler.IEventHandlerAsync`1";
    private const string CallHandlerMetadataName = "LayerBase.Call.ILayerCallHandler`2";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var ownerLayerRegistrations = context.SyntaxProvider
                                             .ForAttributeWithMetadataName(
                                                 OwnerLayerAttributeName,
                                                 static (node, _) => node is ClassDeclarationSyntax,
                                                 static (ctx,  _) => CreateRegistrations(ctx))
                                             .SelectMany(static (items, _) => items);

        var mountMembers = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      MountAttributeName,
                                      static (node, _) => node is VariableDeclaratorSyntax or FieldDeclarationSyntax or PropertyDeclarationSyntax,
                                      static (ctx,  _) => ctx.TargetSymbol);

        var combined = ownerLayerRegistrations.Collect()
                                              .Combine(mountMembers.Collect());

        var compilationAndData = context.CompilationProvider.Combine(combined);

        context.RegisterSourceOutput(compilationAndData, static (spc, source) =>
        {
            var compilation = source.Left;
            var data = source.Right;
            var ownerLayerList = data.Left;
            var mountMemberList = data.Right;

            Execute(spc, compilation, ownerLayerList, mountMemberList);
        });
    }

    private static void Execute(SourceProductionContext spc, Compilation compilation,
                                ImmutableArray<ServiceRegistration> ownerLayers,
                                ImmutableArray<ISymbol> mountMembers)
    {
        var iServiceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
        var iLayerContextSymbol = compilation.GetTypeByMetadataName(ILayerContextMetadataName);
        var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
        var eventHandlerSymbol = compilation.GetTypeByMetadataName(EventHandlerMetadataName);
        var eventHandlerAsyncSymbol = compilation.GetTypeByMetadataName(EventHandlerAsyncMetadataName);
        var callHandlerSymbol = compilation.GetTypeByMetadataName(CallHandlerMetadataName);

        if (iServiceSymbol == null || layerSymbol == null) return;

        var classMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);

        ClassInfo GetOrAddClass(INamedTypeSymbol symbol)
        {
            if (classMap.TryGetValue(symbol, out var info)) return info;
            info = new ClassInfo(symbol);
            classMap[symbol] = info;
            return info;
        }

        foreach (var reg in ownerLayers)
        {
            var info = GetOrAddClass(reg.ServiceType);
            info.OwnerLayerRegistrations.Add(reg);
        }

        foreach (var member in mountMembers)
        {
            if (member.ContainingType == null) continue;
            var info = GetOrAddClass(member.ContainingType);
            info.MountMembers.Add(member);
        }

        var callHandlerRegistrations = new List<CallHandlerRegistration>();

        foreach (var info in classMap.Values)
        {
            var isLayer = InheritsFromLayer(info.Symbol, layerSymbol);
            var isService = ImplementsInterface(info.Symbol, iServiceSymbol);

            // Validation for [Mount]
            if (info.MountMembers.Count > 0)
            {
                if (!IsPartial(info.Symbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        info.MountMembers[0].Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }

                if (isLayer)
                {
                    ProcessLayerMounts(spc, info, iServiceSymbol, classMap);
                }
                else if (isService)
                {
                    ProcessServiceMounts(spc, info, iLayerContextSymbol);
                }
                else if (ImplementsInterface(info.Symbol, iLayerContextSymbol))
                {
                    // Allow [Mount] on ILayerContext classes for member injection.
                    // No auto-registration is performed for these.
                }
                else
                {
                    foreach (var member in info.MountMembers)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountTypeMismatch, member.Locations[0],
                            member.Name, info.Symbol.Name, GetSymbolType(member)?.Name ?? "unknown"));
                    }
                }
            }

            // Process OwnerLayer registrations for call handlers
            foreach (var reg in info.OwnerLayerRegistrations)
            {
                var implementsEventHandler = ImplementsInterface(info.Symbol, eventHandlerSymbol) ||
                                             ImplementsInterface(info.Symbol, eventHandlerAsyncSymbol) ||
                                             ImplementsInterfaceByMetadataName(info.Symbol,
                                                 EventHandlerMetadataName) ||
                                             ImplementsInterfaceByMetadataName(info.Symbol,
                                                 EventHandlerAsyncMetadataName);
                var implementsCallHandler = ImplementsInterface(info.Symbol, callHandlerSymbol) ||
                                            ImplementsInterfaceByMetadataName(info.Symbol, CallHandlerMetadataName);

                if (!isService)
                {
                    if (implementsCallHandler)
                    {
                        foreach (var impl in GetCallHandlerInterfaces(info.Symbol, callHandlerSymbol))
                            callHandlerRegistrations.Add(new CallHandlerRegistration(
                                info.Symbol,
                                reg.LayerType,
                                impl.RequestType,
                                impl.ResponseType,
                                reg.Location ?? info.Symbol.Locations.FirstOrDefault()));
                        continue;
                    }

                    if (implementsEventHandler) continue;

                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceMustImplementIService,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }

                if (!InheritsFromLayer(reg.LayerType, layerSymbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustInheritLayer,
                        reg.Location ?? reg.LayerType.Locations.FirstOrDefault(),
                        reg.LayerType.ToDisplayString()));
                    continue;
                }

                if (!IsPartial(reg.LayerType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        reg.Location ?? reg.LayerType.Locations.FirstOrDefault(),
                        reg.LayerType.ToDisplayString()));
                    continue;
                }

                if (!HasAccessibleParameterlessConstructor(info.Symbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceNeedsPublicParameterlessConstructor,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }

                if (info.Symbol.IsAbstract)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceCannotBeAbstract,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }
            }
        }

        // Check call handler conflicts
        var conflictingRequests = callHandlerRegistrations
                                  .GroupBy(static binding => binding.RequestType, SymbolEqualityComparer.Default)
                                  .Select(static group => new
                                  {
                                      RequestType = group.Key,
                                      Count = group.Count(),
                                      Bindings = group.Select(static binding =>
                                                          new CallBindingSignature(binding.LayerType,
                                                              binding.ResponseType))
                                                      .Distinct(CallBindingSignatureComparer.Instance)
                                                      .OrderBy(static binding =>
                                                          binding.LayerType.ToDisplayString())
                                                      .ThenBy(static binding =>
                                                          binding.ResponseType.ToDisplayString())
                                                      .ToList()
                                  })
                                  .Where(static entry => entry.Count > 1)
                                  .ToDictionary(static entry => entry.RequestType, static entry => entry.Bindings,
                                      SymbolEqualityComparer.Default);

        foreach (var registration in callHandlerRegistrations)
        {
            if (!conflictingRequests.TryGetValue(registration.RequestType, out var bindings)) continue;

            var bindingList = string.Join(", ", bindings.Select(static binding =>
                $"{binding.LayerType.ToDisplayString()} -> {binding.ResponseType.ToDisplayString()}"));

            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.RequestMustHaveSingleBinding,
                registration.Location ?? registration.ServiceType.Locations.FirstOrDefault(),
                registration.RequestType.ToDisplayString(),
                bindingList));
        }

        // Group by Layer for code generation
        var layerGroups = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

        foreach (var info in classMap.Values)
        {
            if (InheritsFromLayer(info.Symbol, layerSymbol))
            {
                if (!layerGroups.ContainsKey(info.Symbol))
                    layerGroups[info.Symbol] = new List<INamedTypeSymbol>();
            }

            foreach (var reg in info.OwnerLayerRegistrations)
            {
                if (!ImplementsInterface(info.Symbol, iServiceSymbol) &&
                    !ImplementsInterface(info.Symbol, callHandlerSymbol)) continue;

                if (!layerGroups.TryGetValue(reg.LayerType, out var list))
                    layerGroups[reg.LayerType] = list = new List<INamedTypeSymbol>();
                list.Add(info.Symbol);
            }
        }

        // Generate Layer Partials
        foreach (var kvp in layerGroups)
        {
            var layerType = kvp.Key;
            var ownerLayerServices = kvp.Value.Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>().ToList();
            
            var injectServices = new List<INamedTypeSymbol>();
            if (classMap.TryGetValue(layerType, out var layerInfo))
            {
                foreach (var member in layerInfo.MountMembers)
                {
                    var type = GetSymbolType(member);
                    if (type is INamedTypeSymbol s && ImplementsInterface(s, iServiceSymbol))
                    {
                        injectServices.Add(s);
                    }
                }
            }

            if (injectServices.Count > 0 || ownerLayerServices.Count > 0)
            {
                var ownerOnlyServices = ownerLayerServices.Where(s => !injectServices.Contains(s, SymbolEqualityComparer.Default)).ToList();
                if (injectServices.Count > 0 && ownerOnlyServices.Count > 0)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerOnlyUnorderedTail, layerType.Locations.FirstOrDefault(),
                        layerType.ToDisplayString(), string.Join(", ", ownerOnlyServices.Select(s => s.ToDisplayString()))));
                }

                var sourceText = GenerateLayerPartial(layerType, injectServices, ownerLayerServices, iServiceSymbol, callHandlerSymbol);
                if (!string.IsNullOrEmpty(sourceText))
                {
                    spc.AddSource(CreateHintName(layerType), SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        }

        // Generate IAutoServiceMount for Services
        foreach (var info in classMap.Values)
        {
            if (ImplementsInterface(info.Symbol, iServiceSymbol) && !InheritsFromLayer(info.Symbol, layerSymbol))
            {
                var mountedContexts = new List<INamedTypeSymbol>();
                foreach (var member in info.MountMembers)
                {
                    var type = GetSymbolType(member);
                    if (type is INamedTypeSymbol s && iLayerContextSymbol != null && 
                        ImplementsInterface(s, iLayerContextSymbol) && !s.IsAbstract && s.TypeKind == TypeKind.Class)
                    {
                        if (!mountedContexts.Contains(s, SymbolEqualityComparer.Default))
                        {
                            mountedContexts.Add(s);
                        }
                    }
                }

                if (mountedContexts.Count > 0)
                {
                    var sourceText = GenerateServicePartial(info.Symbol, mountedContexts);
                    if (!string.IsNullOrEmpty(sourceText))
                    {
                        spc.AddSource(CreateHintName(info.Symbol), SourceText.From(sourceText, Encoding.UTF8));
                    }
                }
            }
        }
    }

    private static void ProcessLayerMounts(SourceProductionContext spc, ClassInfo info, INamedTypeSymbol iServiceSymbol, Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        // Deterministic ordering and partial check
        var declarations = info.MountMembers
                               .Select(f => f.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                                             .AncestorsAndSelf().OfType<TypeDeclarationSyntax>()
                                             .FirstOrDefault())
                               .Distinct()
                               .ToList();

        if (declarations.Count > 1)
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountFieldsMustBeInSameDeclaration,
                info.MountMembers[0].Locations.FirstOrDefault(),
                info.Symbol.ToDisplayString()));

        // Sort by location within the same declaration
        info.MountMembers.Sort((a, b) =>
            a.Locations[0].SourceSpan.Start.CompareTo(b.Locations[0].SourceSpan.Start));

        var mountedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol fieldType) continue;

            if (!mountedTypes.Add(fieldType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.DuplicateMount, member.Locations[0],
                    member.Name, fieldType.ToDisplayString(), info.Symbol.ToDisplayString()));
                continue;
            }

            if (classMap.TryGetValue(fieldType, out var targetInfo))
            {
                var hasMismatch = targetInfo.OwnerLayerRegistrations.Any(r => !SymbolEqualityComparer.Default.Equals(r.LayerType, info.Symbol));
                if (hasMismatch)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountOwnerConflict, member.Locations[0],
                        fieldType.ToDisplayString(), info.Symbol.ToDisplayString()));
                }
            }

            if (!ImplementsInterface(fieldType, iServiceSymbol))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountTypeMismatch, member.Locations[0],
                    member.Name, info.Symbol.Name, fieldType.Name));
                continue;
            }

            if (!HasAccessibleParameterlessConstructor(fieldType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceNeedsPublicParameterlessConstructor,
                    member.Locations[0],
                    fieldType.ToDisplayString()));
            }

            if (fieldType.IsAbstract)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceCannotBeAbstract,
                    member.Locations[0],
                    fieldType.ToDisplayString()));
            }
        }
    }

    private static void ProcessServiceMounts(SourceProductionContext spc, ClassInfo info, INamedTypeSymbol? iLayerContextSymbol)
    {
        // Sort for deterministic generation (though not strictly required for correctness here)
        info.MountMembers.Sort((a, b) =>
            a.Locations[0].SourceSpan.Start.CompareTo(b.Locations[0].SourceSpan.Start));

        var mountedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol contextType) continue;

            if (!mountedTypes.Add(contextType))
            {
                // Duplicate mounting same type is allowed in service, generator will de-duplicate TryAddScoped
                continue;
            }

            if (iLayerContextSymbol != null && ImplementsInterface(contextType, iLayerContextSymbol))
            {
                if (contextType.IsAbstract || contextType.TypeKind != TypeKind.Class)
                {
                    // interface / abstract not supported for auto-registration
                    continue;
                }
            }
            else
            {
                // Not an ILayerContext, but has [Mount]. 
                // Currently [Mount] is only defined for IService (on Layer) or ILayerContext (on IService).
                // If it's something else, we might want a diagnostic, but existing logic used to just skip or warn.
                // The guide says: 不自动注册 interface / abstract 类型字段。 不自动注册普通非 ILayerContext 依赖。
            }
        }
    }

    private static ITypeSymbol? GetSymbolType(ISymbol symbol)
    {
        return symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };
    }

    private static string GenerateLayerPartial(INamedTypeSymbol layerType, List<INamedTypeSymbol> injectServices, List<INamedTypeSymbol> ownerLayerServices, INamedTypeSymbol iServiceSymbol, INamedTypeSymbol? callHandlerSymbol)
    {
        var layerDisplayName = layerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var layerIdentifier = layerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceSymbol = layerType.ContainingNamespace;
        var @namespace = namespaceSymbol is { IsGlobalNamespace: false }
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using LayerBase.Layers;");
        builder.AppendLine("using LayerBase.DI;");

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(layerIdentifier)
               .AppendLine(" : global::LayerBase.DI.IAutoLayerMount");
        builder.AppendLine("{");
        
        builder.AppendLine("    void global::LayerBase.DI.IAutoLayerMount.__AutoMountServices(global::LayerBase.Layers.Layer layerInstance)");
        builder.AppendLine("    {");
        builder.Append("        var typedLayer = (").Append(layerDisplayName).AppendLine(")layerInstance;");

        void EmitRegistration(INamedTypeSymbol svc)
        {
            var serviceDisplay = svc.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (ImplementsInterface(svc, iServiceSymbol))
            {
                builder.Append("        typedLayer.RegisterService((global::LayerBase.DI.IService)new ")
                       .Append(serviceDisplay)
                       .AppendLine("());");
            }
            else if (callHandlerSymbol != null)
            {
                foreach (var impl in GetCallHandlerInterfaces(svc, callHandlerSymbol))
                {
                    var reqDisplay = impl.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var respDisplay = impl.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    builder.Append("        typedLayer.RegisterCallHandler<")
                           .Append(reqDisplay).Append(", ").Append(respDisplay)
                           .Append(">((global::LayerBase.Call.ILayerCallHandler<")
                           .Append(reqDisplay).Append(", ").Append(respDisplay)
                           .Append(">)new ")
                           .Append(serviceDisplay)
                           .AppendLine("());");
                }
            }
        }

        foreach (var service in injectServices)
        {
            EmitRegistration(service);
        }

        foreach (var service in ownerLayerServices)
        {
            if (injectServices.Contains(service, SymbolEqualityComparer.Default)) continue;
            EmitRegistration(service);
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateServicePartial(INamedTypeSymbol serviceType, List<INamedTypeSymbol> mountedContexts)
    {
        var serviceIdentifier = serviceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceSymbol = serviceType.ContainingNamespace;
        var @namespace = namespaceSymbol is { IsGlobalNamespace: false }
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using LayerBase.DI;");

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(serviceIdentifier)
               .AppendLine(" : global::LayerBase.DI.IAutoServiceMount");
        builder.AppendLine("{");
        
        builder.AppendLine("    void global::LayerBase.DI.IAutoServiceMount.__AutoMountContexts(global::LayerBase.DI.IServiceCollection services)");
        builder.AppendLine("    {");

        foreach (var context in mountedContexts)
        {
            var typeName = context.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("        services.TryAddScoped<").Append(typeName).Append(", ").Append(typeName).AppendLine(">();");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static bool HasStaticConstructor(INamedTypeSymbol type)
    {
        return type.GetMembers().OfType<IMethodSymbol>().Any(m => m.MethodKind == MethodKind.StaticConstructor);
    }

    private static string CreateHintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = new StringBuilder(name.Length);
        foreach (var ch in name) sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return $"{sanitized}.AutoMount.g.cs";
    }

    private static ImmutableArray<ServiceRegistration> CreateRegistrations(GeneratorAttributeSyntaxContext context)
    {
        var serviceSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var builder = ImmutableArray.CreateBuilder<ServiceRegistration>();

        foreach (var attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol layerSymbol) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new ServiceRegistration(serviceSymbol, layerSymbol, location));
        }

        return builder.ToImmutable();
    }

    private static bool ImplementsInterface(INamedTypeSymbol? type, INamedTypeSymbol? interfaceSymbol)
    {
        if (type == null || interfaceSymbol == null) return false;

        return type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol));
    }

    private static bool ImplementsInterfaceByMetadataName(INamedTypeSymbol type, string metadataName)
    {
        if (string.IsNullOrWhiteSpace(metadataName)) return false;

        return type.AllInterfaces.Any(i =>
        {
            var candidate = i.OriginalDefinition is INamedTypeSymbol named ? named : i;
            var display = candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return display switch
                   {
                       "global::LayerBase.Call.ILayerCallHandler<TRequest, TResponse>" when
                           metadataName == CallHandlerMetadataName => true,
                       "global::LayerBase.Core.EventHandler.IEventHandler<TValue>" when
                           metadataName == EventHandlerMetadataName => true,
                       "global::LayerBase.Core.EventHandler.IEventHandlerAsync<TValue>" when
                           metadataName == EventHandlerAsyncMetadataName => true,
                       _ => false
                   };
        });
    }

    private static IEnumerable<CallHandlerImplementation> GetCallHandlerInterfaces(INamedTypeSymbol handlerType,
        INamedTypeSymbol?                                                                           callHandlerSymbol)
    {
        if (callHandlerSymbol == null) yield break;

        foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
        {
            if (!SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, callHandlerSymbol)) continue;
            if (iface.TypeArguments.Length != 2) continue;

            yield return new CallHandlerImplementation(iface.TypeArguments[0], iface.TypeArguments[1]);
        }
    }

    private static bool InheritsFromLayer(INamedTypeSymbol? target, INamedTypeSymbol layerSymbol)
    {
        for (var current = target; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, layerSymbol))
                return true;

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
                typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;

        return false;
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length != 0 || ctor.IsStatic) continue;

            if (ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                or Accessibility.ProtectedOrInternal) return true;
        }

        return false;
    }

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "LayerServiceGenerator";

        public static readonly DiagnosticDescriptor ServiceMustImplementIService =
            new(
                "LBG001",
                "OwnerLayer type must implement a supported contract",
                "Type '{0}' is marked with OwnerLayer but does not implement any supported OwnerLayer contract (IService, ILayerCallHandler<TRequest, TResponse>, IEventHandler<TEvent>, or IEventHandlerAsync<TEvent>)",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor LayerMustInheritLayer =
            new(
                "LBG002",
                "OwnerLayer target must derive from Layer",
                "Type '{0}' is not a Layer and cannot be used with OwnerLayerAttribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor LayerMustBePartial =
            new(
                "LBG003",
                "Type must be partial",
                "Type '{0}' must be declared as partial to allow generator to emit registrations",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ServiceNeedsPublicParameterlessConstructor =
            new(
                "LBG004",
                "Service needs parameterless constructor",
                "Service '{0}' must have a public or internal parameterless constructor",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ServiceCannotBeAbstract =
            new(
                "LBG005",
                "Service cannot be abstract",
                "Service '{0}' cannot be abstract when used with OwnerLayerAttribute or MountAttribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor RequestMustHaveSingleBinding =
            new(
                "LBG006",
                "Call request must map to exactly one target and one response",
                "Call request '{0}' has multiple call bindings: {1}. Call is only for single-target functional slices, so each request must map to exactly one layer and one response.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountFieldsMustBeInSameDeclaration =
            new(
                "LBG007",
                "Inject members must be in the same declaration",
                "Type '{0}' is partial, but [Mount] members are scattered across multiple declarations. All [Mount] members must be in the same declaration for deterministic ordering.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountTypeMismatch =
            new(
                "LBG008",
                "Inject type mismatch",
                "Member '{0}' in '{1}' has [Mount] but its type '{2}' is not allowed.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor DuplicateMount =
            new(
                "LBG009",
                "Duplicate Mount",
                "Member '{0}' mounts type '{1}' which is already mounted in '{2}'.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountOwnerConflict =
            new(
                "LBG010",
                "Mount Owner Conflict",
                "Mounted type '{0}' explicitly declares an Owner that is not '{1}'.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerOnlyUnorderedTail =
            new(
                "LBG011",
                "Owner-only trailing items",
                "Type '{0}' has Mount members but also has owner-only registrations ({1}) which will be appended without guaranteed ordering.",
                Category,
                DiagnosticSeverity.Warning,
                true);
    }
#pragma warning restore RS2008

    private sealed class ClassInfo
    {
        public INamedTypeSymbol Symbol { get; }
        public List<ISymbol> MountMembers { get; } = new();
        public List<ServiceRegistration> OwnerLayerRegistrations { get; } = new();

        public ClassInfo(INamedTypeSymbol symbol) => Symbol = symbol;
    }

    private sealed class ServiceRegistration
    {
        public ServiceRegistration(INamedTypeSymbol serviceType, INamedTypeSymbol layerType, Location? location)
        {
            ServiceType = serviceType;
            LayerType = layerType;
            Location = location;
        }
        public INamedTypeSymbol ServiceType { get; }
        public INamedTypeSymbol LayerType { get; }
        public Location? Location { get; }
    }

    private sealed class CallHandlerRegistration
    {
        public CallHandlerRegistration(INamedTypeSymbol serviceType, INamedTypeSymbol layerType,
                                       ITypeSymbol      requestType,
                                       ITypeSymbol      responseType, Location? location)
        {
            ServiceType = serviceType;
            LayerType = layerType;
            RequestType = requestType;
            ResponseType = responseType;
            Location = location;
        }
        public INamedTypeSymbol ServiceType { get; }
        public INamedTypeSymbol LayerType { get; }
        public ITypeSymbol RequestType { get; }
        public ITypeSymbol ResponseType { get; }
        public Location? Location { get; }
    }

    private readonly record struct CallHandlerImplementation(ITypeSymbol RequestType, ITypeSymbol ResponseType);
    private readonly record struct CallBindingSignature(INamedTypeSymbol LayerType, ITypeSymbol ResponseType);

    private sealed class CallBindingSignatureComparer : IEqualityComparer<CallBindingSignature>
    {
        public static readonly CallBindingSignatureComparer Instance = new();
        public bool Equals(CallBindingSignature x, CallBindingSignature y) =>
            SymbolEqualityComparer.Default.Equals(x.LayerType, y.LayerType) && SymbolEqualityComparer.Default.Equals(x.ResponseType, y.ResponseType);
        public int GetHashCode(CallBindingSignature obj) =>
            (SymbolEqualityComparer.Default.GetHashCode(obj.LayerType) * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.ResponseType);
    }
}
