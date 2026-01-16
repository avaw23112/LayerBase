using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator
{
	[Generator(LanguageNames.CSharp)]
	public sealed class LayerServiceGenerator : IIncrementalGenerator
	{
		private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
		private const string IServiceMetadataName = "LayerBase.DI.IService";
		private const string LayerMetadataName = "LayerBase.Layers.Layer";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(static ctx =>
			{
				ctx.AddSource("OwnerLayerAttribute.g.cs", SourceText.From(OwnerLayerAttributeSource, Encoding.UTF8));
			});

			var registrations = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					OwnerLayerAttributeName,
					predicate: static (node, _) => node is ClassDeclarationSyntax,
					transform: static (ctx, ct) => CreateRegistrations(ctx))
				.SelectMany(static (items, _) => items);

			var compilationAndRegistrations = context.CompilationProvider.Combine(registrations.Collect());

			context.RegisterSourceOutput(compilationAndRegistrations, static (spc, source) =>
			{
				var compilation = source.Left;
				var collected = source.Right;

				var iServiceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
				var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);

				if (iServiceSymbol == null || layerSymbol == null)
				{
					return;
				}

				List<ServiceRegistration> validRegistrations = new List<ServiceRegistration>();
				foreach (var registration in collected)
				{
					var serviceSymbol = registration.ServiceType;
					var targetLayer = registration.LayerType;

					if (!ImplementsInterface(serviceSymbol, iServiceSymbol))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceMustImplementIService, registration.Location ?? serviceSymbol.Locations.FirstOrDefault(), serviceSymbol.ToDisplayString()));
						continue;
					}

					if (!InheritsFromLayer(targetLayer, layerSymbol))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustInheritLayer, registration.Location ?? targetLayer.Locations.FirstOrDefault(), targetLayer.ToDisplayString()));
						continue;
					}

					if (!IsPartial(targetLayer))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial, registration.Location ?? targetLayer.Locations.FirstOrDefault(), targetLayer.ToDisplayString()));
						continue;
					}

					if (!HasAccessibleParameterlessConstructor(serviceSymbol))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceNeedsPublicParameterlessConstructor, registration.Location ?? serviceSymbol.Locations.FirstOrDefault(), serviceSymbol.ToDisplayString()));
						continue;
					}

					if (serviceSymbol.IsAbstract)
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ServiceCannotBeAbstract, registration.Location ?? serviceSymbol.Locations.FirstOrDefault(), serviceSymbol.ToDisplayString()));
						continue;
					}

					validRegistrations.Add(registration);
				}

				var groupedByLayer = validRegistrations
					.GroupBy(r => r.LayerType, SymbolEqualityComparer.Default);

				foreach (var group in groupedByLayer)
				{
					if (group.Key is not INamedTypeSymbol layerKey)
					{
						continue;
					}

					var sourceText = GenerateLayerPartial(layerKey, group);
					if (string.IsNullOrEmpty(sourceText))
					{
						continue;
					}
					spc.AddSource(CreateHintName(layerKey), SourceText.From(sourceText, Encoding.UTF8));
				}
			});
		}

		private static ImmutableArray<ServiceRegistration> CreateRegistrations(GeneratorAttributeSyntaxContext context)
		{
			var serviceSymbol = (INamedTypeSymbol)context.TargetSymbol;
			var builder = ImmutableArray.CreateBuilder<ServiceRegistration>();

			foreach (var attribute in context.Attributes)
			{
				if (attribute.ConstructorArguments.Length != 1)
				{
					continue;
				}

				if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol layerSymbol)
				{
					continue;
				}

				var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
				builder.Add(new ServiceRegistration(serviceSymbol, layerSymbol, location));
			}

			return builder.ToImmutable();
		}

		private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceSymbol)
		{
			return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
		}

		private static bool InheritsFromLayer(INamedTypeSymbol target, INamedTypeSymbol layerSymbol)
		{
			for (var current = target; current != null; current = current.BaseType)
			{
				if (SymbolEqualityComparer.Default.Equals(current, layerSymbol))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsPartial(INamedTypeSymbol type)
		{
			foreach (var reference in type.DeclaringSyntaxReferences)
			{
				if (reference.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
				    typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
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
				if (ctor.Parameters.Length != 0 || ctor.IsStatic)
				{
					continue;
				}

				if (ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal)
				{
					return true;
				}
			}

			return false;
		}

		private static string GenerateLayerPartial(INamedTypeSymbol layerType, IEnumerable<ServiceRegistration> registrations)
		{
			var services = (registrations ?? Enumerable.Empty<ServiceRegistration>())
				.Where(r => r?.ServiceType != null)
				.Select(r => r.ServiceType!)
				.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
				.OrderBy(s => s.ToDisplayString())
				.ToList();

			if (services.Count == 0)
			{
				return string.Empty;
			}

			string layerDisplayName = layerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			string layerIdentifier = layerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			var namespaceSymbol = layerType.ContainingNamespace;
			string? @namespace = namespaceSymbol is { IsGlobalNamespace: false }
				? namespaceSymbol.ToDisplayString()
				: null;

			var builder = new StringBuilder();
			builder.AppendLine("// <auto-generated />");
			builder.AppendLine("// This file was generated by LayerServiceGenerator.");
			builder.AppendLine("using LayerBase.Layers;");

			if (!string.IsNullOrEmpty(@namespace))
			{
				builder.Append("namespace ").Append(@namespace).AppendLine();
				builder.AppendLine("{");
			}

			builder.Append("partial class ").Append(layerIdentifier).AppendLine();
			builder.AppendLine("{");
			builder.Append("    static ").Append(layerType.Name).AppendLine("()");
			builder.AppendLine("    {");
			builder.Append("        LayerServiceRegistry.Register(typeof(").Append(layerDisplayName).AppendLine("), static layerInstance =>");
			builder.AppendLine("        {");
			builder.Append("            var typedLayer = (").Append(layerDisplayName).AppendLine(")layerInstance;");

			foreach (var service in services)
			{
				var serviceDisplay = service.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				builder.Append("            typedLayer.RegisterService(new ").Append(serviceDisplay).AppendLine("());");
			}

			builder.AppendLine("        });");
			builder.AppendLine("    }");
			builder.AppendLine("}");

			if (!string.IsNullOrEmpty(@namespace))
			{
				builder.AppendLine("}");
			}

			return builder.ToString();
		}

		private static string CreateHintName(INamedTypeSymbol layerType)
		{
			var name = layerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var sanitized = new StringBuilder(name.Length);
			foreach (var ch in name)
			{
				sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
			}
			return $"{sanitized}.LayerServices.g.cs";
		}

#pragma warning disable RS2008 // Enable analyzer release tracking
		private static class Diagnostics
		{
			private const string Category = "LayerServiceGenerator";

			public static readonly DiagnosticDescriptor ServiceMustImplementIService =
				new DiagnosticDescriptor(
					id: "LBG001",
					title: "Service must implement IService",
					messageFormat: "Type '{0}' is marked with OwnerLayer but does not implement LayerBase.DI.IService",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor LayerMustInheritLayer =
				new DiagnosticDescriptor(
					id: "LBG002",
					title: "OwnerLayer target must derive from Layer",
					messageFormat: "Type '{0}' is not a Layer and cannot be used with OwnerLayerAttribute",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor LayerMustBePartial =
				new DiagnosticDescriptor(
					id: "LBG003",
					title: "Layer must be partial",
					messageFormat: "Layer '{0}' must be declared as partial to allow generator to emit registrations",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor ServiceNeedsPublicParameterlessConstructor =
				new DiagnosticDescriptor(
					id: "LBG004",
					title: "Service needs parameterless constructor",
					messageFormat: "Service '{0}' must have a public or internal parameterless constructor",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor ServiceCannotBeAbstract =
				new DiagnosticDescriptor(
					id: "LBG005",
					title: "Service cannot be abstract",
					messageFormat: "Service '{0}' cannot be abstract when used with OwnerLayerAttribute",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);
		}
#pragma warning restore RS2008

		private static readonly string OwnerLayerAttributeSource = @"// <auto-generated />
using System;

namespace LayerBase.Layers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class OwnerLayerAttribute : Attribute
    {
        public OwnerLayerAttribute(Type layerType)
        {
            LayerType = layerType ?? throw new ArgumentNullException(nameof(layerType));
        }

        public Type LayerType { get; }
    }
}
";

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
	}
}
