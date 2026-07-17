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
public sealed class LayerServiceGenerator : IIncrementalGenerator
{
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string OwnerServiceAttributeName = "LayerBase.DI.Options.OwnerServiceAttribute";
    private const string MountAttributeName = "LayerBase.DI.Options.MountAttribute";
    private const string LayerToolAttributeName = "LayerBase.Tools.LayerToolAttribute";
    private const string EventMetaDataBaseName = "LayerBase.Event.EventMetaData.EventMetaData`1";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ILayerContextMetadataName = "LayerBase.DI.ILayerContext";
    private const string LayerMetadataName = "LayerBase.Layers.Layer";
    private const string EventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandler`1";
    private const string EventHandlerAsyncMetadataName = "LayerBase.Core.EventHandler.IEventHandlerAsync`1";
    private const string CallHandlerMetadataName = "LayerBase.Call.IScopeLocalCallHandler`2";
    private const string ScopeAttributeNamespace = "LayerBase.Scope";
    private const string ScopeAttributeMetadataName = "ScopeAttribute`1";

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

        var layerToolAttributes = context.SyntaxProvider
                                         .ForAttributeWithMetadataName(
                                             LayerToolAttributeName,
                                             static (node, _) => node is ClassDeclarationSyntax,
                                             static (ctx, _) => CreateLayerToolAttributeInfo(ctx))
                                         .Where(static item => item is not null)
                                         .Select(static (item, _) => item!);

        var layerToolCandidates = context.SyntaxProvider
                                         .CreateSyntaxProvider(
                                             static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                                             static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node))
                                         .Where(static symbol => symbol is INamedTypeSymbol)
                                         .Select(static (symbol, _) => (INamedTypeSymbol)symbol!);

        var layerToolData = layerToolAttributes.Collect()
                                              .Combine(layerToolCandidates.Collect());

        var serviceAndMountData = ownerServiceRegistrations.Collect()
                                                          .Combine(mountMembers.Collect()
                                                                              .Combine(layerToolData));
        var combined = ownerLayerRegistrations.Collect()
                                              .Combine(serviceAndMountData);

        var compilationAndData = context.CompilationProvider.Combine(combined);

        context.RegisterSourceOutput(compilationAndData, static (spc, source) =>
        {
            var compilation = source.Left;
            var data = source.Right;
            var ownerLayerList = data.Left;
            var ownerServiceList = data.Right.Left;
            var mountMemberList = data.Right.Right.Left;
            var layerToolAttributeList = data.Right.Right.Right.Left;
            var layerToolCandidateList = data.Right.Right.Right.Right;

            Execute(spc, compilation, ownerLayerList, ownerServiceList, mountMemberList,
                layerToolAttributeList, layerToolCandidateList);
        });
    }

    private static void Execute(SourceProductionContext spc, Compilation compilation,
                                ImmutableArray<ServiceRegistration> ownerLayers,
                                ImmutableArray<ServiceContextRegistration> ownerServices,
                                ImmutableArray<ISymbol> mountMembers,
                                ImmutableArray<LayerToolAttributeInfo> layerToolAttributes,
                                ImmutableArray<INamedTypeSymbol> layerToolCandidates)
    {
        var iServiceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
        var iLayerContextSymbol = compilation.GetTypeByMetadataName(ILayerContextMetadataName);
        var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
        var eventHandlerSymbol = compilation.GetTypeByMetadataName(EventHandlerMetadataName);
        var eventHandlerAsyncSymbol = compilation.GetTypeByMetadataName(EventHandlerAsyncMetadataName);
        var callHandlerSymbol = compilation.GetTypeByMetadataName(CallHandlerMetadataName);
        var eventMetaDataSymbol = compilation.GetTypeByMetadataName(EventMetaDataBaseName);
        var layerTools = CreateLayerToolRegistrations(layerToolAttributes, layerToolCandidates);

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
        var validatedLayerToolRegistrations = new List<LayerToolRegistration>();

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
                if (!IsPartial(info.Symbol))
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
                    info.ProcessedMountedContexts =
                        ProcessServiceMounts(spc, info, iServiceSymbol, iLayerContextSymbol, classMap);
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
                var ownerLayerInCurrentAssembly =
                    SymbolEqualityComparer.Default.Equals(reg.LayerType.ContainingAssembly, compilation.Assembly);

                if (!InheritsFromLayer(reg.LayerType, layerSymbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustInheritLayer,
                        reg.Location ?? reg.LayerType.Locations.FirstOrDefault(),
                        reg.LayerType.ToDisplayString()));
                    continue;
                }

                if (ownerLayerInCurrentAssembly && !IsPartial(reg.LayerType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        reg.Location ?? reg.LayerType.Locations.FirstOrDefault(),
                        reg.LayerType.ToDisplayString()));
                    continue;
                }

                if (implementsCallHandler)
                {
                    if (!ownerLayerInCurrentAssembly)
                    {
                        continue;
                    }

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

                if (IsEventMetaDataType(info.Symbol, eventMetaDataSymbol))
                {
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

                if (!ownerLayerInCurrentAssembly)
                {
                    continue;
                }

                validatedOwnerLayerRegistrations.Add(reg);
            }

            foreach (var reg in info.OwnerServiceRegistrations)
            {
                var ownerServiceInCurrentAssembly =
                    SymbolEqualityComparer.Default.Equals(reg.ServiceType.ContainingAssembly, compilation.Assembly);

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

                if (ownerServiceInCurrentAssembly && !IsPartial(reg.ServiceType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        reg.Location ?? reg.ServiceType.Locations.FirstOrDefault(),
                        reg.ServiceType.ToDisplayString()));
                    continue;
                }

                if (!ownerServiceInCurrentAssembly)
                {
                    continue;
                }

                validatedOwnerServiceRegistrations.Add(reg);
            }
        }

        foreach (var tool in layerTools)
        {
            if (!SymbolEqualityComparer.Default.Equals(tool.OwnerLayerType.ContainingAssembly, compilation.Assembly))
            {
                continue;
            }

            GetOrAddClass(tool.OwnerLayerType);

            if (!InheritsFromLayer(tool.OwnerLayerType, layerSymbol))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustInheritLayer,
                    tool.Location ?? tool.OwnerLayerType.Locations.FirstOrDefault(),
                    tool.OwnerLayerType.ToDisplayString()));
                continue;
            }

            if (!IsPartial(tool.OwnerLayerType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                    tool.Location ?? tool.OwnerLayerType.Locations.FirstOrDefault(),
                    tool.OwnerLayerType.ToDisplayString()));
                continue;
            }

            if (!IsAssignableFrom(tool.ContractType, tool.ImplementationType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerToolImplementationNotAssignable,
                    tool.Location ?? tool.ImplementationType.Locations.FirstOrDefault(),
                    tool.ImplementationType.ToDisplayString(),
                    tool.ContractType.ToDisplayString()));
                continue;
            }

            validatedLayerToolRegistrations.Add(tool);
        }

        var layerGroups = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        var layerToolGroups = validatedLayerToolRegistrations
                              .GroupBy(static registration => registration.OwnerLayerType, SymbolEqualityComparer.Default)
                              .ToDictionary(static group => group.Key,
                                  static group => group.ToList(),
                                  SymbolEqualityComparer.Default);

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
            layerToolGroups.TryGetValue(layerType, out var ownerLayerTools);
            ownerLayerTools ??= new List<LayerToolRegistration>();

            var injectServices = new List<MountedContext>();
            if (classMap.TryGetValue(layerType, out var layerInfo))
            {
                injectServices.AddRange(layerInfo.LayerMountedServices);
            }

            if (injectServices.Count == 0 && ownerLayerServices.Count == 0 && ownerLayerTools.Count == 0)
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

            var mountInjections = layerInfo?.ProcessedMountInjections ?? new List<MountInjection>();
            var sourceText = GenerateLayerPartial(layerType, injectServices, ownerLayerServices, mountInjections,
                ownerLayerTools, iServiceSymbol, callHandlerSymbol, classMap);
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

            if (combinedContexts.Count == 0)
            {
                continue;
            }

            var sourceText = GenerateServicePartial(info.Symbol, combinedContexts, info.ProcessedMountInjections);
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
        info.ProcessedMountInjections.Clear();

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol serviceType) continue;

            var implType = GetMountImplementationType(member);

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
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountDependencyInAnotherLayer, member.Locations[0],
                        actualImplType.ToDisplayString(), info.Symbol.ToDisplayString()));
                    continue;
                }
            }

            var targetScope = GetEffectiveScope(actualImplType, classMap);
            if (targetScope != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMountsNonMainScopeObject, member.Locations[0],
                    info.Symbol.ToDisplayString(), actualImplType.ToDisplayString(), targetScope.ToDisplayString()));
                continue;
            }

            if (!ImplementsInterface(actualImplType, iServiceSymbol))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountTypeMismatch, member.Locations[0],
                    member.Name, info.Symbol.Name, actualImplType.Name));
                continue;
            }

            info.LayerMountedServices.Add(new MountedContext(serviceType, actualImplType));
            info.ProcessedMountInjections.Add(new MountInjection(member, serviceType));
        }
    }

    private static List<MountedContext> ProcessServiceMounts(SourceProductionContext spc, ClassInfo info,
                                                             INamedTypeSymbol iServiceSymbol,
                                                             INamedTypeSymbol? iLayerContextSymbol,
                                                             Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        info.MountMembers.Sort((a, b) =>
            a.Locations[0].SourceSpan.Start.CompareTo(b.Locations[0].SourceSpan.Start));

        var results = new List<MountedContext>();
        var seenPairs = new HashSet<MountedContext>(MountedContextComparer.Instance);
        info.ProcessedMountInjections.Clear();

        foreach (var member in info.MountMembers)
        {
            var type = GetSymbolType(member);
            if (type is not INamedTypeSymbol serviceType) continue;

            var implType = GetMountImplementationType(member);

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

            var implementsService = ImplementsInterface(actualImplType, iServiceSymbol) ||
                                    ImplementsInterfaceByMetadataName(actualImplType, IServiceMetadataName);
            var implementsLayerContext =
                iLayerContextSymbol != null && ImplementsInterface(actualImplType, iLayerContextSymbol) ||
                ImplementsInterfaceByMetadataName(actualImplType, ILayerContextMetadataName);

            if (!implementsService && !implementsLayerContext)
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
                var parentLayer = GetEffectiveOwnerLayer(info.Symbol, classMap);
                var targetLayer = GetEffectiveOwnerLayer(actualImplType, classMap);
                if (parentLayer != null &&
                    targetLayer != null &&
                    !SymbolEqualityComparer.Default.Equals(parentLayer, targetLayer))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountDependencyInAnotherLayer,
                        member.Locations[0], actualImplType.ToDisplayString(), parentLayer.ToDisplayString()));
                    continue;
                }

                if (implementsLayerContext)
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
            }

            var parentScope = GetEffectiveScope(info.Symbol, classMap);
            var targetScope = GetEffectiveScope(actualImplType, classMap);
            if (!SameScope(parentScope, targetScope))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MountDependencyInAnotherScope,
                    member.Locations[0],
                    actualImplType.ToDisplayString(),
                    ToScopeDisplay(parentScope),
                    ToScopeDisplay(targetScope)));
                continue;
            }

            var mountedContext = new MountedContext(serviceType, actualImplType);
            if (seenPairs.Add(mountedContext))
            {
                results.Add(mountedContext);
            }

            info.ProcessedMountInjections.Add(new MountInjection(member, serviceType));
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

    private static INamedTypeSymbol? GetMountImplementationType(ISymbol member)
    {
        var attribute = member.GetAttributes()
                              .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MountAttributeName);
        if (attribute == null)
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is INamedTypeSymbol constructorType)
        {
            return constructorType;
        }

        foreach (var argument in attribute.NamedArguments)
        {
            if ((argument.Key == "Implementation" || argument.Key == "ImplementationType") &&
                argument.Value.Value is INamedTypeSymbol namedType)
            {
                return namedType;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetEffectiveOwnerLayer(
        INamedTypeSymbol type,
        Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        if (!classMap.TryGetValue(type, out var info))
        {
            return null;
        }

        var directLayer = info.OwnerLayerRegistrations
                              .Select(static registration => registration.LayerType)
                              .FirstOrDefault();
        if (directLayer != null)
        {
            return directLayer;
        }

        foreach (var ownerService in info.OwnerServiceRegistrations.Select(static registration => registration.ServiceType))
        {
            var ownerLayer = GetEffectiveOwnerLayer(ownerService, classMap);
            if (ownerLayer != null)
            {
                return ownerLayer;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetEffectiveScope(
        INamedTypeSymbol type,
        Dictionary<INamedTypeSymbol, ClassInfo> classMap)
    {
        var explicitScope = GetExplicitScope(type);
        if (explicitScope != null)
        {
            return explicitScope;
        }

        if (!classMap.TryGetValue(type, out var info))
        {
            return null;
        }

        foreach (var ownerService in info.OwnerServiceRegistrations.Select(static registration => registration.ServiceType))
        {
            var ownerScope = GetEffectiveScope(ownerService, classMap);
            if (ownerScope != null)
            {
                return ownerScope;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetExplicitScope(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
            {
                continue;
            }

            var original = attributeClass.OriginalDefinition;
            if (original.ContainingNamespace.ToDisplayString() != ScopeAttributeNamespace)
            {
                continue;
            }

            if (original.MetadataName == ScopeAttributeMetadataName &&
                attributeClass.TypeArguments.Length == 1)
            {
                return attributeClass.TypeArguments[0] as INamedTypeSymbol;
            }

            if (original.MetadataName == "ScopeAttribute" &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is INamedTypeSymbol scopeType)
            {
                return scopeType;
            }
        }

        return null;
    }

    private static bool SameScope(INamedTypeSymbol? left, INamedTypeSymbol? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return SymbolEqualityComparer.Default.Equals(left, right);
    }

    private static string ToScopeDisplay(INamedTypeSymbol? scope)
    {
        return scope?.ToDisplayString() ?? "LayerBase.Scope.MainScope";
    }

    private static string GenerateLayerPartial(INamedTypeSymbol layerType, List<MountedContext> injectServices,
                                               List<INamedTypeSymbol> ownerLayerServices,
                                               List<MountInjection> mountInjections,
                                               IReadOnlyList<LayerToolRegistration> ownerLayerTools,
                                               INamedTypeSymbol iServiceSymbol, INamedTypeSymbol? callHandlerSymbol,
                                               Dictionary<INamedTypeSymbol, ClassInfo> classMap)
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

        var ownerScopeTypes = ownerLayerServices
                              .Select(service => GetEffectiveScope(service, classMap))
                              .Concat(ownerLayerTools.Select(static tool => tool.OwnerScopeType))
                              .Where(static scope => scope != null)
                              .Distinct(SymbolEqualityComparer.Default)
                              .Cast<INamedTypeSymbol>()
                              .OrderBy(static scope => scope.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                              .ToList();

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(layerIdentifier)
               .Append(" : global::LayerBase.DI.IAutoLayerMount, global::LayerBase.DI.IGeneratedMountInject");
        if (ownerLayerTools.Count > 0)
        {
            builder.Append(", global::LayerBase.Tools.IGeneratedLayerToolProvider");
        }
        if (ownerScopeTypes.Count > 0)
        {
            builder.Append(", global::LayerBase.Scope.IGeneratedScopeDefinitionProvider");
        }

        builder.AppendLine();
        builder.AppendLine("{");

        builder.AppendLine(
            "    void global::LayerBase.DI.IAutoLayerMount.__AutoMountServices(global::LayerBase.Layers.Layer layerInstance)");
        builder.AppendLine("    {");
        builder.Append("        var typedLayer = (").Append(layerDisplayName).AppendLine(")layerInstance;");

        void EmitRegistration(INamedTypeSymbol exposedType, INamedTypeSymbol implType)
        {
            var serviceDisplay = exposedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implDisplay = implType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var ownerScope = GetEffectiveScope(implType, classMap);
            var ownerScopeDisplay = ownerScope?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (ImplementsInterface(implType, iServiceSymbol) || ImplementsInterfaceByMetadataName(implType, IServiceMetadataName))
            {
                builder.Append("        typedLayer.RegisterService(typeof(")
                       .Append(serviceDisplay)
                       .Append("), (global::LayerBase.DI.IService)new ")
                       .Append(implDisplay)
                       .Append("()");
                if (ownerScopeDisplay != null)
                {
                    builder.Append(", typeof(")
                           .Append(ownerScopeDisplay)
                           .Append(")");
                }

                builder.AppendLine(");");
            }
            else if (callHandlerSymbol != null)
            {
                foreach (var impl in GetCallHandlerInterfaces(implType, callHandlerSymbol))
                {
                    var reqDisplay = impl.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var respDisplay = impl.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    builder.Append("        typedLayer.RegisterCallHandler<")
                           .Append(reqDisplay).Append(", ").Append(respDisplay)
                           .Append(">((global::LayerBase.Call.IScopeLocalCallHandler<")
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

        EmitMountInjectionMethod(builder, mountInjections);

        if (ownerLayerTools.Count > 0)
        {
            EmitLayerToolContributionProvider(builder, layerDisplayName, ownerLayerTools);
        }

        if (ownerScopeTypes.Count > 0)
        {
            EmitScopeDefinitionProvider(builder, ownerScopeTypes);
        }

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateServicePartial(INamedTypeSymbol serviceType, List<MountedContext> mountedContexts,
                                                 List<MountInjection> mountInjections)
    {
        var serviceIdentifier = serviceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceSymbol = serviceType.ContainingNamespace;
        var @namespace = namespaceSymbol is { IsGlobalNamespace: false }
            ? namespaceSymbol.ToDisplayString()
            : null;
        var eventHandlers = mountedContexts
                            .SelectMany(static (context, contextIndex) =>
                                GetEventHandlerInterfaces(context.ImplementationType)
                                    .Select((handler, handlerIndex) => new GeneratedServiceEventHandler(
                                        contextIndex,
                                        handlerIndex,
                                        context.ServiceType,
                                        context.ImplementationType,
                                        handler.EventType,
                                        handler.IsAsync)))
                            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using LayerBase.DI;");

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(serviceIdentifier)
               .Append(" : global::LayerBase.DI.IAutoServiceMount, global::LayerBase.DI.IGeneratedMountInject");
        if (eventHandlers.Count > 0)
        {
            builder.Append(", global::LayerBase.DI.IAutoSubscribe");
        }

        builder.AppendLine();
        builder.AppendLine("{");

        foreach (var handler in eventHandlers)
        {
            builder.Append("    private ")
                   .Append(handler.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                   .Append(" __layerBaseEventHandler")
                   .Append(handler.ContextIndex)
                   .Append('_')
                   .Append(handler.HandlerIndex)
                   .AppendLine(" = null!;");
        }

        if (eventHandlers.Count > 0)
        {
            builder.AppendLine();
        }

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

        EmitMountInjectionMethod(builder, mountInjections, eventHandlers);

        if (eventHandlers.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(
                "    void global::LayerBase.DI.IAutoSubscribe.AutoBind(global::LayerBase.Layers.Layer layer)");
            builder.AppendLine("    {");
            foreach (var handler in eventHandlers)
            {
                var eventType = handler.EventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var method = handler.IsAsync ? "SubscribeAsync" : "SubscribeFlow";
                var contract = handler.IsAsync
                    ? "global::LayerBase.Core.EventHandler.IEventHandlerAsync"
                    : "global::LayerBase.Core.EventHandler.IEventHandler";

                builder.Append("        layer.")
                       .Append(method)
                       .Append('<')
                       .Append(eventType)
                       .Append(">((")
                       .Append(contract)
                       .Append('<')
                       .Append(eventType)
                       .Append(">)this.__layerBaseEventHandler")
                       .Append(handler.ContextIndex)
                       .Append('_')
                       .Append(handler.HandlerIndex)
                       .AppendLine(");");
                builder.Append("        layer.RecordSubscribedEvent(typeof(")
                       .Append(eventType)
                       .AppendLine("));");
            }

            builder.AppendLine("    }");

            builder.AppendLine();
            builder.AppendLine(
                "    global::System.Collections.Generic.IEnumerable<global::LayerBase.DI.EventDependency> global::LayerBase.DI.IAutoSubscribe.GetEventDependencies()");
            builder.AppendLine("    {");
            builder.AppendLine("        yield break;");
            builder.AppendLine("    }");

            builder.AppendLine();
            builder.AppendLine(
                "    global::System.Collections.Generic.IEnumerable<global::System.Type> global::LayerBase.DI.IAutoSubscribe.GetSubscribedEvents()");
            builder.AppendLine("    {");
            foreach (var eventType in eventHandlers.Select(static handler => handler.EventType)
                                                   .Distinct(SymbolEqualityComparer.Default)
                                                   .Cast<ITypeSymbol>())
            {
                builder.Append("        yield return typeof(")
                       .Append(eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                       .AppendLine(");");
            }

            builder.AppendLine("        yield break;");
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static void EmitScopeDefinitionProvider(
        StringBuilder builder,
        IReadOnlyList<INamedTypeSymbol> ownerScopeTypes)
    {
        var definitions = new List<(string TypeName, string Identity, int ScopeId)>();

        foreach (var scopeType in ownerScopeTypes)
        {
            string typeName = scopeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string identity = ScopeDefinitionCodeGen.BuildIdentity(scopeType);
            int scopeId = ScopeDefinitionCodeGen.ComputeScopeId(identity);
            definitions.Add((typeName, identity, scopeId));
        }

        definitions.Sort(static (a, b) =>
        {
            int cmp = a.ScopeId.CompareTo(b.ScopeId);
            if (cmp != 0) return cmp;
            cmp = string.Compare(a.Identity, b.Identity, StringComparison.Ordinal);
            if (cmp != 0) return cmp;
            return string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal);
        });

        builder.AppendLine();
        builder.AppendLine(
            "    private static readonly global::LayerBase.Scope.GeneratedScopeDefinition[] __LayerBaseScopeDefinitions =");
        builder.AppendLine("    {");

        foreach (var (typeName, identity, scopeId) in definitions)
        {
            builder.Append("        new global::LayerBase.Scope.GeneratedScopeDefinition(");
            builder.Append("scopeId: ").Append(scopeId).Append(", ");
            builder.Append("identity: \"").Append(identity).Append("\", ");
            builder.Append("scopeType: typeof(").Append(typeName).Append("), ");
            builder.Append("factory: static () => new ").Append(typeName).Append("())");
            builder.AppendLine(",");
        }

        builder.AppendLine("    };");
        builder.AppendLine();
        builder.AppendLine(
            "    global::LayerBase.Scope.GeneratedScopeDefinition[] " +
            "global::LayerBase.Scope.IGeneratedScopeDefinitionProvider.__GetScopeDefinitions()");
        builder.AppendLine("    {");
        builder.AppendLine("        return __LayerBaseScopeDefinitions;");
        builder.AppendLine("    }");
    }

    private static void EmitLayerToolContributionProvider(
        StringBuilder builder,
        string layerDisplayName,
        IReadOnlyList<LayerToolRegistration> ownerLayerTools)
    {
        builder.AppendLine();
        builder.AppendLine(
            "    global::LayerBase.Modules.LayerToolContribution[] global::LayerBase.Tools.IGeneratedLayerToolProvider.__GetLayerToolContributions()");
        builder.AppendLine("    {");
        builder.AppendLine("        return new global::LayerBase.Modules.LayerToolContribution[]");
        builder.AppendLine("        {");

        foreach (var tool in ownerLayerTools.OrderBy(static tool => tool.ContractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                                            .ThenBy(static tool => tool.LocalKey, StringComparer.Ordinal)
                                            .ThenBy(static tool => tool.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal))
        {
            var contractDisplay = tool.ContractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implementationDisplay = tool.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var ownerScopeDisplay = tool.OwnerScopeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.AppendLine("            global::LayerBase.Modules.LayerToolContribution.ForTypes(");
            builder.Append("                typeof(").Append(contractDisplay).AppendLine("),");
            builder.Append("                typeof(").Append(implementationDisplay).AppendLine("),");
            builder.Append("                \"").Append(Escape(tool.LocalKey)).AppendLine("\",");
            builder.Append("                typeof(").Append(layerDisplayName).AppendLine("),");
            builder.Append("                typeof(").Append(ownerScopeDisplay).AppendLine("),");
            builder.Append("                ").Append(tool.Cache ? "true" : "false").AppendLine("),");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");
    }

    private static void EmitMountInjectionMethod(
        StringBuilder builder,
        List<MountInjection> mountInjections,
        IReadOnlyList<GeneratedServiceEventHandler>? eventHandlers = null)
    {
        builder.AppendLine();
        builder.AppendLine(
            "    void global::LayerBase.DI.IGeneratedMountInject.__InjectMounts(global::LayerBase.DI.IServiceProvider services)");
        builder.AppendLine("    {");

        foreach (var injection in mountInjections)
        {
            var serviceName = injection.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.Append("        this.")
                   .Append(injection.Member.Name)
                   .Append(" = services.Get<")
                   .Append(serviceName)
                   .AppendLine(">();");
        }

        if (eventHandlers != null)
        {
            foreach (var handler in eventHandlers)
            {
                var serviceName = handler.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                builder.Append("        this.__layerBaseEventHandler")
                       .Append(handler.ContextIndex)
                       .Append('_')
                       .Append(handler.HandlerIndex)
                       .Append(" = services.Get<")
                       .Append(serviceName)
                       .AppendLine(">();");
            }
        }

        builder.AppendLine("    }");
    }

    private static string CreateHintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = new StringBuilder(name.Length);
        foreach (var ch in name) sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return $"{sanitized}.AutoMount.g.cs";
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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

    private static LayerToolAttributeInfo? CreateLayerToolAttributeInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol attributeType)
        {
            return null;
        }

        var layerToolAttribute = context.Attributes.FirstOrDefault(static attribute =>
            IsAttribute(attribute, LayerToolAttributeName));
        if (layerToolAttribute == null)
        {
            return null;
        }

        var ownerLayerType = ReadTypeConstructorArgument(layerToolAttribute, 1);
        var ownerScopeType = ReadTypeConstructorArgument(layerToolAttribute, 2);
        if (ownerLayerType == null || ownerScopeType == null)
        {
            return null;
        }

        var contractType = ReadTypeNamedArgument(layerToolAttribute, "Contract");
        var keyProperty = ReadStringNamedArgument(layerToolAttribute, "DefaultKeyProperty");
        if (string.IsNullOrWhiteSpace(keyProperty))
        {
            keyProperty = "Key";
        }

        var allowCache = ReadBoolNamedArgument(layerToolAttribute, "AllowCache") ?? true;
        var location = layerToolAttribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
        return new LayerToolAttributeInfo(attributeType, ownerLayerType, ownerScopeType, contractType, keyProperty!, allowCache, location);
    }

    private static ImmutableArray<LayerToolRegistration> CreateLayerToolRegistrations(
        ImmutableArray<LayerToolAttributeInfo> toolAttributes,
        ImmutableArray<INamedTypeSymbol> candidateTypes)
    {
        if (toolAttributes.IsDefaultOrEmpty || candidateTypes.IsDefaultOrEmpty)
        {
            return ImmutableArray<LayerToolRegistration>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<LayerToolRegistration>();
        foreach (var implementationType in candidateTypes)
        {
            foreach (var attribute in implementationType.GetAttributes())
            {
                var toolInfo = FindLayerToolAttribute(toolAttributes, attribute.AttributeClass);
                if (toolInfo == null)
                {
                    continue;
                }

                var contractType = toolInfo.ContractType ?? implementationType;
                var localKey = ReadStringValue(attribute, toolInfo.KeyProperty)
                               ?? ReadStringValue(attribute, "LocalKey")
                               ?? "default";
                if (string.IsNullOrWhiteSpace(localKey))
                {
                    localKey = "default";
                }

                var cache = toolInfo.AllowCache && (ReadBoolNamedArgument(attribute, "Cache") ?? true);
                var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
                builder.Add(new LayerToolRegistration(
                    implementationType,
                    toolInfo.OwnerLayerType,
                    toolInfo.OwnerScopeType,
                    contractType,
                    localKey,
                    cache,
                    location));
            }
        }

        return builder.ToImmutable();
    }

    private static LayerToolAttributeInfo? FindLayerToolAttribute(
        ImmutableArray<LayerToolAttributeInfo> toolAttributes,
        INamedTypeSymbol? attributeType)
    {
        if (attributeType == null)
        {
            return null;
        }

        foreach (var toolAttribute in toolAttributes)
        {
            if (SymbolEqualityComparer.Default.Equals(toolAttribute.AttributeType, attributeType))
            {
                return toolAttribute;
            }
        }

        return null;
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal);
    }

    private static string? ReadStringNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadStringValue(AttributeData attribute, string propertyName)
    {
        var named = ReadStringNamedArgument(attribute, propertyName);
        if (named != null)
        {
            return named;
        }

        if (propertyName == "Key" &&
            attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string key)
        {
            return key;
        }

        return null;
    }

    private static bool? ReadBoolNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is bool value)
            {
                return value;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? ReadTypeNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is INamedTypeSymbol value)
            {
                return value;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? ReadTypeValue(AttributeData attribute, string propertyName)
    {
        return ReadTypeNamedArgument(attribute, propertyName);
    }

    private static INamedTypeSymbol? ReadTypeConstructorArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as INamedTypeSymbol
            : null;
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
                "global::LayerBase.Call.IScopeLocalCallHandler<TRequest, TResponse>" when metadataName == CallHandlerMetadataName => true,
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

    private static IEnumerable<EventHandlerImplementation> GetEventHandlerInterfaces(INamedTypeSymbol handlerType)
    {
        foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
        {
            if (iface.TypeArguments.Length != 1) continue;

            var display = iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (display == "global::LayerBase.Core.EventHandler.IEventHandler<TValue>")
            {
                yield return new EventHandlerImplementation(iface.TypeArguments[0], false);
            }
            else if (display == "global::LayerBase.Core.EventHandler.IEventHandlerAsync<TValue>")
            {
                yield return new EventHandlerImplementation(iface.TypeArguments[0], true);
            }
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

    private static bool IsEventMetaDataType(INamedTypeSymbol type, INamedTypeSymbol? eventMetaDataSymbol)
    {
        if (eventMetaDataSymbol == null)
            return false;

        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, eventMetaDataSymbol))
            {
                return true;
            }
        }

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
                "Type '{0}' is marked with OwnerLayer but does not implement any supported OwnerLayer contract (IService or IScopeLocalCallHandler<TRequest, TResponse>)",
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
                "Mount implementation type must implement IService or ILayerContext",
                "Mount implementation type '{0}' must implement IService or ILayerContext to be auto-registered from IService.",
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

        public static readonly DiagnosticDescriptor LayerMountsNonMainScopeObject =
            new(
                "LBMOUNT007",
                "Layer object cannot mount non-MainScope object",
                "Layer '{0}' cannot mount '{1}' because it belongs to Scope '{2}'. Use ScopeEvent or ScopeCall for cross-scope access.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountDependencyInAnotherScope =
            new(
                "LBMOUNT008",
                "Mount dependency belongs to another Scope",
                "Mounted type '{0}' belongs to Scope '{2}' but the parent belongs to Scope '{1}'. Use ScopeEvent or ScopeCall for cross-scope access.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountDependencyInAnotherLayer =
            new(
                "LBMOUNT009",
                "Mount dependency belongs to another Layer",
                "Mounted type '{0}' does not belong to Layer '{1}'. Use this.Call<Request, Response>(...) for cross-layer access.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MountTargetIsLayerTool =
            new(
                "LBMOUNT010",
                "LayerTool cannot be mounted",
                "Mounted type '{0}' is a LayerTool. Use this.Tools().GetOrCreate<Tool>() instead of [Mount].",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor LayerToolImplementationNotAssignable =
            new(
                "LBTOOL001",
                "LayerTool implementation must implement contract",
                "LayerTool implementation '{0}' must implement contract '{1}'",
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
        public List<MountInjection> ProcessedMountInjections { get; } = new();
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

    private sealed class LayerToolRegistration
    {
        public LayerToolRegistration(
            INamedTypeSymbol implementationType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol ownerScopeType,
            INamedTypeSymbol contractType,
            string localKey,
            bool cache,
            Location? location)
        {
            ImplementationType = implementationType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ContractType = contractType;
            LocalKey = localKey;
            Cache = cache;
            Location = location;
        }

        public INamedTypeSymbol ImplementationType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol OwnerScopeType { get; }

        public INamedTypeSymbol ContractType { get; }

        public string LocalKey { get; }

        public bool Cache { get; }

        public Location? Location { get; }
    }

    private sealed class LayerToolAttributeInfo
    {
        public LayerToolAttributeInfo(
            INamedTypeSymbol attributeType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol ownerScopeType,
            INamedTypeSymbol? contractType,
            string keyProperty,
            bool allowCache,
            Location? location)
        {
            AttributeType = attributeType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ContractType = contractType;
            KeyProperty = keyProperty;
            AllowCache = allowCache;
            Location = location;
        }

        public INamedTypeSymbol AttributeType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol OwnerScopeType { get; }

        public INamedTypeSymbol? ContractType { get; }

        public string KeyProperty { get; }

        public bool AllowCache { get; }

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

    private readonly record struct EventHandlerImplementation(ITypeSymbol EventType, bool IsAsync);

    private readonly record struct GeneratedServiceEventHandler(
        int ContextIndex,
        int HandlerIndex,
        INamedTypeSymbol ServiceType,
        INamedTypeSymbol ImplementationType,
        ITypeSymbol EventType,
        bool IsAsync);

    private readonly record struct MountedContext(INamedTypeSymbol ServiceType, INamedTypeSymbol ImplementationType);

    private readonly record struct MountInjection(ISymbol Member, INamedTypeSymbol ServiceType);

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
