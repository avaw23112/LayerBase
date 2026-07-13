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
    private const string OwnerServiceAttributeName = "LayerBase.DI.Options.OwnerServiceAttribute";
    private const string MountAttributeName = "LayerBase.DI.Options.MountAttribute";
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
                                                 static (ctx, _) => CreateRegistrations(ctx))
                                             .SelectMany(static (items, _) => items);

        var ownerServiceRegistrations = context.SyntaxProvider
                                               .ForAttributeWithMetadataName(
                                                   OwnerServiceAttributeName,
                                                   static (node, _) => node is ClassDeclarationSyntax,
                                                   static (ctx, _) => CreateOwnerServiceRegistrations(ctx))
                                               .SelectMany(static (items, _) => items);

        var mountMembers = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      MountAttributeName,
                                      static (node, _) => node is VariableDeclaratorSyntax or FieldDeclarationSyntax
                                          or PropertyDeclarationSyntax,
                                      static (ctx, _) => ctx.TargetSymbol);

        var combined = ownerLayerRegistrations.Collect()
                                              .Combine(ownerServiceRegistrations.Collect()
                                                       .Combine(mountMembers.Collect()));

        var compilationAndData = context.CompilationProvider.Combine(combined);

        context.RegisterSourceOutput(compilationAndData, static (spc, source) =>
        {
            var compilation = source.Left;
            var data = source.Right;
            var ownerLayerList = data.Left;
            var ownerServiceList = data.Right.Left;
            var mountMemberList = data.Right.Right;

            Execute(spc, compilation, ownerLayerList, ownerServiceList, mountMemberList);
        });
    }

    private static void Execute(SourceProductionContext spc, Compilation compilation,
                                ImmutableArray<ServiceRegistration> ownerLayers,
                                ImmutableArray<ServiceContextRegistration> ownerServices,
                                ImmutableArray<ISymbol> mountMembers)
    {
        var iServiceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
        var iLayerContextSymbol = compilation.GetTypeByMetadataName(ILayerContextMetadataName);
        var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
        var eventHandlerSymbol = compilation.GetTypeByMetadataName(EventHandlerMetadataName);
        var eventHandlerAsyncSymbol = compilation.GetTypeByMetadataName(EventHandlerAsyncMetadataName);
        var callHandlerSymbol = compilation.GetTypeByMetadataName(CallHandlerMetadataName);

        if (iServiceSymbol == null || layerSymbol == null)
        {
            return;
        }

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
            GetOrAddClass(reg.LayerType);
        }

        foreach (var reg in ownerServices)
        {
            var info = GetOrAddClass(reg.ContextType);
            info.OwnerServiceRegistrations.Add(reg);
            GetOrAddClass(reg.ServiceType);
        }

        foreach (var member in mountMembers)
        {
            if (member.ContainingType == null)
            {
                continue;
            }

            var info = GetOrAddClass(member.ContainingType);
            info.MountMembers.Add(member);
        }

        var callHandlerRegistrations = new List<CallHandlerRegistration>();
        var validatedOwnerLayerRegistrations = new List<ServiceRegistration>();
        var validatedOwnerServiceRegistrations = new List<ServiceContextRegistration>();

        foreach (var info in classMap.Values)
        {
            var isLayer = InheritsFromLayer(info.Symbol, layerSymbol);
            var isService = ImplementsInterface(info.Symbol, iServiceSymbol) ||
                            ImplementsInterfaceByMetadataName(info.Symbol, IServiceMetadataName);
            var isLayerContext = (iLayerContextSymbol != null && ImplementsInterface(info.Symbol, iLayerContextSymbol)) ||
                                 ImplementsInterfaceByMetadataName(info.Symbol, ILayerContextMetadataName);
            var implementsEventHandler =
                (eventHandlerSymbol != null && ImplementsInterface(info.Symbol, eventHandlerSymbol)) ||
                (eventHandlerAsyncSymbol != null && ImplementsInterface(info.Symbol, eventHandlerAsyncSymbol)) ||
                ImplementsInterfaceByMetadataName(info.Symbol, EventHandlerMetadataName) ||
                ImplementsInterfaceByMetadataName(info.Symbol, EventHandlerAsyncMetadataName);
            var implementsCallHandler =
                (callHandlerSymbol != null && ImplementsInterface(info.Symbol, callHandlerSymbol)) ||
                ImplementsInterfaceByMetadataName(info.Symbol, CallHandlerMetadataName);

            if (info.MountMembers.Count > 0)
            {
                if (GeneratorOwnerDiagnostics.HasGenericContainingType(info.Symbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        GeneratorOwnerDiagnostics.GenericOwnerNotSupported,
                        info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                }
                else if (GeneratorOwnerDiagnostics.IsNestedType(info.Symbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        GeneratorOwnerDiagnostics.NestedMountOwnerNotSupported,
                        info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                }
                else if (!IsPartial(info.Symbol))
                {
                    var diagnostic = isLayer ? Diagnostics.MountLayerMustBePartial : Diagnostics.MountServiceMustBePartial;
                    spc.ReportDiagnostic(Diagnostic.Create(diagnostic,
                        info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                }
                else if (isLayer)
                {
                    ProcessLayerMounts(spc, info, iServiceSymbol, classMap);
                }
                else if (isService)
                {
                    info.ProcessedMountedContexts = ProcessServiceMounts(spc, info, iLayerContextSymbol, classMap);
                }
                else if (!isLayerContext)
                {
                    foreach (var member in info.MountMembers)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountTypeMismatch, member.Locations[0],
                            member.Name, info.Symbol.Name, GetSymbolType(member)?.Name ?? "unknown"));
                    }
                }
            }

            foreach (var reg in info.OwnerLayerRegistrations)
            {
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

                if (implementsCallHandler)
                {
                    validatedOwnerLayerRegistrations.Add(reg);
                    foreach (var impl in GetCallHandlerInterfaces(info.Symbol, callHandlerSymbol))
                    {
                        callHandlerRegistrations.Add(new CallHandlerRegistration(
                            info.Symbol,
                            reg.LayerType,
                            impl.RequestType,
                            impl.ResponseType,
                            reg.Location ?? info.Symbol.Locations.FirstOrDefault()));
                    }

                    continue;
                }

                if (!isService)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceMustImplementIService,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
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

                validatedOwnerLayerRegistrations.Add(reg);
            }

            foreach (var reg in info.OwnerServiceRegistrations)
            {
                if (!ImplementsInterface(reg.ServiceType, iServiceSymbol) &&
                    !ImplementsInterfaceByMetadataName(reg.ServiceType, IServiceMetadataName))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerServiceTargetMustImplementIService,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        reg.ServiceType.ToDisplayString()));
                    continue;
                }

                if (implementsCallHandler)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerServiceCallHandlerMustUseOwnerLayer,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }

                if (isLayer || isService || (!isLayerContext && !implementsEventHandler))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerServiceTargetTypeInvalid,
                        reg.Location ?? info.Symbol.Locations.FirstOrDefault(),
                        info.Symbol.ToDisplayString()));
                    continue;
                }

                if (!IsPartial(reg.ServiceType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        reg.Location ?? reg.ServiceType.Locations.FirstOrDefault(),
                        reg.ServiceType.ToDisplayString()));
                    continue;
                }

                validatedOwnerServiceRegistrations.Add(reg);
            }
        }

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

        var layerGroups = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

        foreach (var info in classMap.Values)
        {
            if (InheritsFromLayer(info.Symbol, layerSymbol) && !layerGroups.ContainsKey(info.Symbol))
            {
                layerGroups[info.Symbol] = new List<INamedTypeSymbol>();
            }
        }

        foreach (var reg in validatedOwnerLayerRegistrations)
        {
            if (!layerGroups.TryGetValue(reg.LayerType, out var list))
            {
                list = new List<INamedTypeSymbol>();
                layerGroups[reg.LayerType] = list;
            }

            list.Add(reg.ServiceType);
        }

        foreach (var kvp in layerGroups)
        {
            var layerType = kvp.Key;
            var ownerLayerServices = kvp.Value
                                        .Distinct(SymbolEqualityComparer.Default)
                                        .Cast<INamedTypeSymbol>()
                                        .ToList();

            var injectServices = new List<MountedContext>();
            if (classMap.TryGetValue(layerType, out var layerInfo))
            {
                injectServices.AddRange(layerInfo.LayerMountedServices);
            }

            if (injectServices.Count == 0 && ownerLayerServices.Count == 0)
            {
                continue;
            }

            var ownerOnlyServices = ownerLayerServices.Where(s =>
                !injectServices.Any(m => SymbolEqualityComparer.Default.Equals(m.ImplementationType, s))).ToList();
            if (injectServices.Count > 0 && ownerOnlyServices.Count > 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerOnlyUnorderedTail,
                    layerType.Locations.FirstOrDefault(),
                    layerType.ToDisplayString(),
                    string.Join(", ", ownerOnlyServices.Select(s => s.ToDisplayString()))));
            }

            var sourceText = GenerateLayerPartial(layerType, injectServices, ownerLayerServices, iServiceSymbol,
                callHandlerSymbol);
            if (!string.IsNullOrEmpty(sourceText))
            {
                spc.AddSource(CreateHintName(layerType), SourceText.From(sourceText, Encoding.UTF8));
            }
        }

        var ownerServiceGroups = validatedOwnerServiceRegistrations
                                 .GroupBy(static reg => reg.ServiceType, SymbolEqualityComparer.Default)
                                 .ToDictionary(static group => group.Key,
                                     static group => group.ToList(),
                                     SymbolEqualityComparer.Default);

        foreach (var info in classMap.Values)
        {
            if (!ImplementsInterface(info.Symbol, iServiceSymbol) || InheritsFromLayer(info.Symbol, layerSymbol))
            {
                continue;
            }

            if (!IsPartial(info.Symbol))
            {
                continue;
            }

            var mountedContexts = info.ProcessedMountedContexts ?? new List<MountedContext>();
            ownerServiceGroups.TryGetValue(info.Symbol, out var ownerServiceContexts);
            ownerServiceContexts ??= new List<ServiceContextRegistration>();

            var combinedContexts = CombineMountedAndOwnerServiceContexts(spc, info.Symbol, mountedContexts,
                ownerServiceContexts);

            if (combinedContexts.Count == 0 && info.MountMembers.Count == 0)
            {
                continue;
            }

            var sourceText = GenerateServicePartial(info.Symbol, combinedContexts, info.MountMembers);
            if (!string.IsNullOrEmpty(sourceText))
            {
                spc.AddSource(CreateHintName(info.Symbol), SourceText.From(sourceText, Encoding.UTF8));
            }
        }

        foreach (var info in classMap.Values)
        {
            if (info.MountMembers.Count == 0 ||
                InheritsFromLayer(info.Symbol, layerSymbol) ||
                ImplementsInterface(info.Symbol, iServiceSymbol) ||
                !ImplementsInterfaceByMetadataName(info.Symbol, ILayerContextMetadataName) ||
                !IsPartial(info.Symbol))
            {
                continue;
            }

            var sourceText = GenerateScopeMountPartial(info.Symbol, info.MountMembers);
            if (!string.IsNullOrEmpty(sourceText))
            {
                spc.AddSource(CreateHintName(info.Symbol), SourceText.From(sourceText, Encoding.UTF8));
            }
        }
    }

    private static void ProcessLayerMounts(SourceProductionContext spc, ClassInfo info, INamedTypeSymbol iServiceSymbol,
                                           Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        var declarations = info.MountMembers
                               .Select(f => f.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                                             .AncestorsAndSelf().OfType<TypeDeclarationSyntax>()
                                             .FirstOrDefault())
                               .Distinct()
                               .ToList();

        if (declarations.Count > 1)
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountFieldsMustBeInSameDeclaration,
                info.MountMembers[0].Locations.FirstOrDefault(),
                info.Symbol.ToDisplayString()));
        }

        info.MountMembers.Sort((a, b) =>
            a.Locations[0].SourceSpan.Start.CompareTo(b.Locations[0].SourceSpan.Start));

        var mountedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        info.LayerMountedServices.Clear();

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol serviceType) continue;

            INamedTypeSymbol? implType = null;
            var attribute = member.GetAttributes()
                                  .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MountAttributeName);
            if (attribute != null && attribute.ConstructorArguments.Length > 0)
            {
                implType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            }

            var actualImplType = implType ?? serviceType;

            if (!mountedTypes.Add(serviceType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.DuplicateMount, member.Locations[0],
                    member.Name, serviceType.ToDisplayString(), info.Symbol.ToDisplayString()));
                continue;
            }

            if (actualImplType.IsAbstract || actualImplType.TypeKind != TypeKind.Class)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountImplMustBeConcrete, member.Locations[0],
                    actualImplType.ToDisplayString()));
                continue;
            }

            if (!IsAssignableFrom(serviceType, actualImplType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountImplNotAssignable, member.Locations[0],
                    actualImplType.ToDisplayString(), serviceType.ToDisplayString()));
                continue;
            }

            if (classMap.TryGetValue(actualImplType, out var targetInfo))
            {
                var hasMismatch = targetInfo.OwnerLayerRegistrations.Any(r =>
                    !SymbolEqualityComparer.Default.Equals(r.LayerType, info.Symbol));
                if (hasMismatch)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountOwnerConflict, member.Locations[0],
                        actualImplType.ToDisplayString(), info.Symbol.ToDisplayString()));
                }
            }

            if (!ImplementsInterface(actualImplType, iServiceSymbol))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountTypeMismatch, member.Locations[0],
                    member.Name, info.Symbol.Name, actualImplType.Name));
                continue;
            }

            info.LayerMountedServices.Add(new MountedContext(serviceType, actualImplType));
        }
    }

    private static List<MountedContext> ProcessServiceMounts(SourceProductionContext spc, ClassInfo info,
                                                             INamedTypeSymbol? iLayerContextSymbol,
                                                             Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        info.MountMembers.Sort((a, b) =>
            a.Locations[0].SourceSpan.Start.CompareTo(b.Locations[0].SourceSpan.Start));

        var results = new List<MountedContext>();
        var seenPairs = new HashSet<MountedContext>(MountedContextComparer.Instance);

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol serviceType) continue;

            INamedTypeSymbol? implType = null;
            var attribute = member.GetAttributes()
                                  .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MountAttributeName);
            if (attribute != null && attribute.ConstructorArguments.Length > 0)
            {
                implType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            }

            var actualImplType = implType ?? serviceType;

            if (actualImplType.IsAbstract || actualImplType.TypeKind != TypeKind.Class)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountImplMustBeConcrete, member.Locations[0],
                    actualImplType.ToDisplayString()));
                continue;
            }

            if (!IsAssignableFrom(serviceType, actualImplType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountImplNotAssignable, member.Locations[0],
                    actualImplType.ToDisplayString(), serviceType.ToDisplayString()));
                continue;
            }

            if (iLayerContextSymbol != null && !ImplementsInterface(actualImplType, iLayerContextSymbol) &&
                !ImplementsInterfaceByMetadataName(actualImplType, ILayerContextMetadataName))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountImplMustImplementILayerContext,
                    member.Locations[0], actualImplType.ToDisplayString()));
                continue;
            }

            if (implType == null && (serviceType.IsAbstract || serviceType.TypeKind == TypeKind.Interface))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountFieldTypeInvalid, member.Locations[0],
                    serviceType.ToDisplayString()));
                continue;
            }

            if (classMap.TryGetValue(actualImplType, out var targetInfo))
            {
                var hasMismatch = targetInfo.OwnerServiceRegistrations.Any(r =>
                    !SymbolEqualityComparer.Default.Equals(r.ServiceType, info.Symbol));
                if (hasMismatch)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerServiceConflictsWithExplicitMount,
                        member.Locations[0], actualImplType.ToDisplayString(), info.Symbol.ToDisplayString()));
                    continue;
                }
            }

            var mountedContext = new MountedContext(serviceType, actualImplType);
            if (seenPairs.Add(mountedContext))
            {
                results.Add(mountedContext);
            }
        }

        return results;
    }

    private static List<MountedContext> CombineMountedAndOwnerServiceContexts(SourceProductionContext spc,
        INamedTypeSymbol serviceType,
        List<MountedContext> mountedContexts,
        IReadOnlyList<ServiceContextRegistration> ownerServiceContexts)
    {
        var combined = new List<MountedContext>(mountedContexts);
        var distinctOwnerOnly = new List<INamedTypeSymbol>();

        foreach (var contextType in ownerServiceContexts.Select(static reg => reg.ContextType)
                                                        .Distinct(SymbolEqualityComparer.Default)
                                                        .Cast<INamedTypeSymbol>())
        {
            if (mountedContexts.Any(m => SymbolEqualityComparer.Default.Equals(m.ImplementationType, contextType)))
            {
                continue;
            }

            var mountedContext = new MountedContext(contextType, contextType);
            if (combined.Any(existing => MountedContextComparer.Instance.Equals(existing, mountedContext)))
            {
                continue;
            }

            combined.Add(mountedContext);
            distinctOwnerOnly.Add(contextType);
        }

        if (mountedContexts.Count > 0 && distinctOwnerOnly.Count > 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OwnerServiceUnorderedTail,
                serviceType.Locations.FirstOrDefault(),
                serviceType.ToDisplayString(),
                string.Join(", ", distinctOwnerOnly.Select(static s => s.ToDisplayString()))));
        }

        return combined;
    }

    private static bool IsAssignableFrom(ITypeSymbol target, ITypeSymbol source)
    {
        if (SymbolEqualityComparer.Default.Equals(target, source)) return true;

        if (target is INamedTypeSymbol namedTarget && namedTarget.TypeKind == TypeKind.Interface)
        {
            return source.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, target) ||
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, target));
        }

        for (var current = source.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target) ||
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, target))
                return true;
        }

        return false;
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

    private static ITypeSymbol? GetMountLookupType(ISymbol member)
    {
        var attribute = member.GetAttributes()
                              .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MountAttributeName);
        if (attribute != null && attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is ITypeSymbol implementationType)
        {
            return implementationType;
        }

        return GetSymbolType(member);
    }

    private static bool CanAssignMountMember(ISymbol member)
    {
        return member switch
        {
            IFieldSymbol { IsReadOnly: false, IsConst: false } => true,
            IPropertySymbol property => property.SetMethod != null,
            _ => false
        };
    }

    private static string GenerateLayerPartial(INamedTypeSymbol layerType, List<MountedContext> injectServices,
                                               List<INamedTypeSymbol> ownerLayerServices,
                                               INamedTypeSymbol iServiceSymbol, INamedTypeSymbol? callHandlerSymbol)
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

        builder.AppendLine(
            "    void global::LayerBase.DI.IAutoLayerMount.__AutoMountServices(global::LayerBase.Layers.Layer layerInstance)");
        builder.AppendLine("    {");
        builder.Append("        var typedLayer = (").Append(layerDisplayName).AppendLine(")layerInstance;");

        void EmitRegistration(INamedTypeSymbol exposedType, INamedTypeSymbol implType)
        {
            var serviceDisplay = exposedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implDisplay = implType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (ImplementsInterface(implType, iServiceSymbol) || ImplementsInterfaceByMetadataName(implType, IServiceMetadataName))
            {
                builder.Append("        typedLayer.RegisterService(typeof(")
                       .Append(serviceDisplay)
                       .Append("), (global::LayerBase.DI.IService)new ")
                       .Append(implDisplay)
                       .AppendLine("());");
            }
            else if (callHandlerSymbol != null)
            {
                foreach (var impl in GetCallHandlerInterfaces(implType, callHandlerSymbol))
                {
                    var reqDisplay = impl.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var respDisplay = impl.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    builder.Append("        typedLayer.RegisterCallHandler<")
                           .Append(reqDisplay).Append(", ").Append(respDisplay)
                           .Append(">((global::LayerBase.Call.ILayerCallHandler<")
                           .Append(reqDisplay).Append(", ").Append(respDisplay)
                           .Append(">)new ")
                           .Append(implDisplay)
                           .AppendLine("());");
                }
            }
        }

        foreach (var service in injectServices)
        {
            EmitRegistration(service.ServiceType, service.ImplementationType);
        }

        foreach (var service in ownerLayerServices)
        {
            if (injectServices.Any(m => SymbolEqualityComparer.Default.Equals(m.ImplementationType, service))) continue;
            EmitRegistration(service, service);
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateServicePartial(
        INamedTypeSymbol serviceType,
        List<MountedContext> mountedContexts,
        List<ISymbol> mountMembers)
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
               .Append(" : global::LayerBase.DI.IAutoServiceMount");
        if (mountMembers.Count > 0)
        {
            builder.Append(", global::LayerBase.Scope.DI.IGeneratedScopeMount, global::LayerBase.Scope.DI.IGeneratedScopeMountMetadata");
        }
        builder.AppendLine();
        builder.AppendLine("{");

        builder.AppendLine(
            "    void global::LayerBase.DI.IAutoServiceMount.__AutoMountContexts(global::LayerBase.DI.IServiceCollection services)");
        builder.AppendLine("    {");

        foreach (var context in mountedContexts)
        {
            var serviceName = context.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implName = context.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("        services.TryAddScoped<").Append(serviceName).Append(", ").Append(implName)
                   .AppendLine(">();");
        }

        builder.AppendLine("    }");

        AppendScopeMountMethod(builder, mountMembers);
        AppendScopeMountMetadata(builder, mountMembers);

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateScopeMountPartial(INamedTypeSymbol type, List<ISymbol> mountMembers)
    {
        var typeIdentifier = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceSymbol = type.ContainingNamespace;
        var @namespace = namespaceSymbol is { IsGlobalNamespace: false }
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using LayerBase.Scope.DI;");

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(typeIdentifier)
               .AppendLine(" : global::LayerBase.Scope.DI.IGeneratedScopeMount, global::LayerBase.Scope.DI.IGeneratedScopeMountMetadata");
        builder.AppendLine("{");
        AppendScopeMountMethod(builder, mountMembers);
        AppendScopeMountMetadata(builder, mountMembers);
        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static void AppendScopeMountMethod(StringBuilder builder, List<ISymbol> mountMembers)
    {
        if (mountMembers.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("    void global::LayerBase.Scope.DI.IGeneratedScopeMount.Mount(in global::LayerBase.Scope.DI.ScopeMountContext context)");
        builder.AppendLine("    {");

        int localDependencyId = 0;
        foreach (var member in mountMembers.OrderBy(static member => member.Locations[0].SourceSpan.Start))
        {
            if (!CanAssignMountMember(member)) continue;

            var lookupType = GetMountLookupType(member);
            if (lookupType == null) continue;

            string lookupTypeName = lookupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("        this.")
                   .Append(member.Name)
                   .Append(" = context.GetAt<")
                   .Append(lookupTypeName)
                   .Append(">(")
                   .Append(localDependencyId++)
                   .AppendLine(");");
        }

        builder.AppendLine("    }");
    }

    private static void AppendScopeMountMetadata(StringBuilder builder, List<ISymbol> mountMembers)
    {
        if (mountMembers.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("    global::System.RuntimeTypeHandle[] global::LayerBase.Scope.DI.IGeneratedScopeMountMetadata.GetScopeMountDependencies()");
        builder.AppendLine("    {");
        builder.AppendLine("        return new global::System.RuntimeTypeHandle[]");
        builder.AppendLine("        {");

        foreach (var member in mountMembers.OrderBy(static member => member.Locations[0].SourceSpan.Start))
        {
            if (!CanAssignMountMember(member)) continue;

            var lookupType = GetMountLookupType(member);
            if (lookupType == null) continue;

            string lookupTypeName = lookupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("            typeof(")
                   .Append(lookupTypeName)
                   .AppendLine(").TypeHandle,");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");
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

    private static ImmutableArray<ServiceContextRegistration> CreateOwnerServiceRegistrations(
        GeneratorAttributeSyntaxContext context)
    {
        var contextType = (INamedTypeSymbol)context.TargetSymbol;
        var builder = ImmutableArray.CreateBuilder<ServiceContextRegistration>();

        foreach (var attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new ServiceContextRegistration(contextType, serviceType, location));
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
                "global::LayerBase.DI.IService" when metadataName == IServiceMetadataName => true,
                "global::LayerBase.DI.ILayerContext" when metadataName == ILayerContextMetadataName => true,
                "global::LayerBase.Call.ILayerCallHandler<TRequest, TResponse>" when metadataName == CallHandlerMetadataName => true,
                "global::LayerBase.Core.EventHandler.IEventHandler<TValue>" when metadataName == EventHandlerMetadataName => true,
                "global::LayerBase.Core.EventHandler.IEventHandlerAsync<TValue>" when metadataName == EventHandlerAsyncMetadataName => true,
                _ => false
            };
        });
    }

    private static IEnumerable<CallHandlerImplementation> GetCallHandlerInterfaces(INamedTypeSymbol handlerType,
        INamedTypeSymbol? callHandlerSymbol)
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
                "Type '{0}' is marked with OwnerLayer but does not implement any supported OwnerLayer contract (IService or ILayerCallHandler<TRequest, TResponse>)",
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

        public static readonly DiagnosticDescriptor MountServiceMustBePartial =
            new(
                "LBMOUNT001",
                "IService type must be partial",
                "IService type '{0}' contains [Mount] ILayerContext members and must be declared partial.",
                Category,
                DiagnosticSeverity.Warning,
                true);

        public static readonly DiagnosticDescriptor MountImplMustBeConcrete =
            new(
                "LBMOUNT002",
                "Mount implementation type must be concrete class",
                "Mount implementation type '{0}' must be a concrete class.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountImplNotAssignable =
            new(
                "LBMOUNT003",
                "Mount implementation type not assignable",
                "Mount implementation type '{0}' is not assignable to field type '{1}'.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountImplMustImplementILayerContext =
            new(
                "LBMOUNT004",
                "Mount implementation type must implement ILayerContext",
                "Mount implementation type '{0}' must implement ILayerContext to be auto-registered from IService.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountFieldTypeInvalid =
            new(
                "LBMOUNT005",
                "Mount field type invalid",
                "Mount field type '{0}' is interface or abstract. Use [Mount(typeof(ImplementationType))] or register it manually in ConfigureServices.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountLayerMustBePartial =
            new(
                "LBMOUNT006",
                "Layer type must be partial",
                "Layer type '{0}' contains [Mount] IService members and must be declared partial.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceTargetMustImplementIService =
            new(
                "LBOS001",
                "OwnerService target must implement IService",
                "Type '{0}' is not an IService and cannot be used with OwnerServiceAttribute.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceTargetTypeInvalid =
            new(
                "LBOS002",
                "OwnerService can only be used on supported context types",
                "Type '{0}' uses [OwnerService] but only ILayerContext, IEventHandler<TEvent>, and IEventHandlerAsync<TEvent> types can declare [OwnerService].",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceCallHandlerMustUseOwnerLayer =
            new(
                "LBOS003",
                "CallHandler must use OwnerLayer instead of OwnerService",
                "Type '{0}' uses [OwnerService] but CallHandler should use [OwnerLayer], not [OwnerService]. Call is only a single-target Layer functional slice; if you need multiple layers, aggregation, broadcast, or workflow coordination, model that explicitly instead of widening Call semantics.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceConflictsWithExplicitMount =
            new(
                "LBOS004",
                "OwnerService conflicts with explicit Mount",
                "Mounted type '{0}' is explicitly mounted in service '{1}' but declares [OwnerService] for a different service.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceUnorderedTail =
            new(
                "LBOS005",
                "Owner-only registrations appended after Mount members",
                "Service '{0}' has explicit [Mount] members but also has owner-only registrations ({1}). Owner-only registrations will be appended after mounted members without field-order semantics.",
                Category,
                DiagnosticSeverity.Warning,
                true);
    }
#pragma warning restore RS2008

    private sealed class ClassInfo
    {
        public ClassInfo(INamedTypeSymbol symbol)
        {
            Symbol = symbol;
        }

        public INamedTypeSymbol Symbol { get; }
        public List<ISymbol> MountMembers { get; } = new();
        public List<ServiceRegistration> OwnerLayerRegistrations { get; } = new();
        public List<ServiceContextRegistration> OwnerServiceRegistrations { get; } = new();
        public List<MountedContext> LayerMountedServices { get; } = new();
        public List<MountedContext>? ProcessedMountedContexts { get; set; }
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

    private sealed class ServiceContextRegistration
    {
        public ServiceContextRegistration(INamedTypeSymbol contextType, INamedTypeSymbol serviceType, Location? location)
        {
            ContextType = contextType;
            ServiceType = serviceType;
            Location = location;
        }

        public INamedTypeSymbol ContextType { get; }
        public INamedTypeSymbol ServiceType { get; }
        public Location? Location { get; }
    }

    private sealed class CallHandlerRegistration
    {
        public CallHandlerRegistration(INamedTypeSymbol serviceType, INamedTypeSymbol layerType,
                                       ITypeSymbol requestType,
                                       ITypeSymbol responseType, Location? location)
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

    private readonly record struct MountedContext(INamedTypeSymbol ServiceType, INamedTypeSymbol ImplementationType);

    private sealed class CallBindingSignatureComparer : IEqualityComparer<CallBindingSignature>
    {
        public static readonly CallBindingSignatureComparer Instance = new();

        public bool Equals(CallBindingSignature x, CallBindingSignature y) =>
            SymbolEqualityComparer.Default.Equals(x.LayerType, y.LayerType) &&
            SymbolEqualityComparer.Default.Equals(x.ResponseType, y.ResponseType);

        public int GetHashCode(CallBindingSignature obj) =>
            (SymbolEqualityComparer.Default.GetHashCode(obj.LayerType) * 397) ^
            SymbolEqualityComparer.Default.GetHashCode(obj.ResponseType);
    }

    private sealed class MountedContextComparer : IEqualityComparer<MountedContext>
    {
        public static readonly MountedContextComparer Instance = new();

        public bool Equals(MountedContext x, MountedContext y) =>
            SymbolEqualityComparer.Default.Equals(x.ServiceType, y.ServiceType) &&
            SymbolEqualityComparer.Default.Equals(x.ImplementationType, y.ImplementationType);

        public int GetHashCode(MountedContext obj) =>
            (SymbolEqualityComparer.Default.GetHashCode(obj.ServiceType) * 397) ^
            SymbolEqualityComparer.Default.GetHashCode(obj.ImplementationType);
    }
}
