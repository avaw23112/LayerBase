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
	public sealed class LayerEventHandlerGenerator : IIncrementalGenerator
	{
		private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
		private const string LayerMetadataName = "LayerBase.Layers.Layer";
		private const string EventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandler`1";
		private const string EventHandlerAsyncMetadataName = "LayerBase.Core.EventHandler.IEventHandlerAsync`1";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var registrations = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					OwnerLayerAttributeName,
					predicate: static (node, _) => node is ClassDeclarationSyntax,
					transform: static (ctx, _) => CreateRegistrations(ctx))
				.SelectMany(static (items, _) => items);

			var compilationAndRegistrations = context.CompilationProvider.Combine(registrations.Collect());

			context.RegisterSourceOutput(compilationAndRegistrations, static (spc, source) =>
			{
				var compilation = source.Left;
				var collected = source.Right;

				var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
				var eventHandlerSymbol = compilation.GetTypeByMetadataName(EventHandlerMetadataName);
				var eventHandlerAsyncSymbol = compilation.GetTypeByMetadataName(EventHandlerAsyncMetadataName);

				if (layerSymbol == null || (eventHandlerSymbol == null && eventHandlerAsyncSymbol == null))
				{
					return;
				}

				List<HandlerBinding> validBindings = new List<HandlerBinding>();
				foreach (var registration in collected)
				{
					var handlerType = registration.HandlerType;
					var targetLayer = registration.LayerType;
					var location = registration.Location ?? handlerType.Locations.FirstOrDefault();

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

					if (handlerType.IsAbstract)
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.HandlerCannotBeAbstract, location, handlerType.ToDisplayString()));
						continue;
					}

					if (!HasAccessibleParameterlessConstructor(handlerType))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.HandlerNeedsPublicParameterlessConstructor, location, handlerType.ToDisplayString()));
						continue;
					}

					var implementations = GetEventHandlerInterfaces(handlerType, eventHandlerSymbol, eventHandlerAsyncSymbol).ToList();
					if (implementations.Count == 0)
					{
						continue;
					}

					foreach (var impl in implementations)
					{
						validBindings.Add(new HandlerBinding(handlerType, targetLayer, impl.EventType, impl.Kind, location));
					}
				}

				var groupedByLayer = validBindings
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

		private static ImmutableArray<HandlerRegistration> CreateRegistrations(GeneratorAttributeSyntaxContext context)
		{
			var handlerSymbol = (INamedTypeSymbol)context.TargetSymbol;
			var builder = ImmutableArray.CreateBuilder<HandlerRegistration>();

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
				builder.Add(new HandlerRegistration(handlerSymbol, layerSymbol, location));
			}

			return builder.ToImmutable();
		}

		private static IEnumerable<EventHandlerImplementation> GetEventHandlerInterfaces(INamedTypeSymbol handlerType, INamedTypeSymbol? syncInterface, INamedTypeSymbol? asyncInterface)
		{
			if (syncInterface == null && asyncInterface == null)
			{
				yield break;
			}

			foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
			{
				if (iface.TypeArguments.Length != 1)
				{
					continue;
				}

				var eventType = iface.TypeArguments[0];
				if (syncInterface != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, syncInterface))
				{
					yield return new EventHandlerImplementation(eventType, EventHandlerKind.Sync);
				}
				else if (asyncInterface != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, asyncInterface))
				{
					yield return new EventHandlerImplementation(eventType, EventHandlerKind.Async);
				}
			}
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

		private static string GenerateLayerPartial(INamedTypeSymbol layerType, IEnumerable<HandlerBinding> bindings)
		{
			var handlers = (bindings ?? Enumerable.Empty<HandlerBinding>())
				.Where(b => b?.HandlerType != null && b.EventType != null)
				.Distinct(HandlerBindingComparer.Instance)
				.OrderBy(b => b.HandlerType.ToDisplayString())
				.ThenBy(b => b.EventType?.ToDisplayString())
				.ThenBy(b => b.Kind)
				.ToList();

			if (handlers.Count == 0)
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
			builder.AppendLine("// This file was generated by LayerEventHandlerGenerator.");
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

			foreach (var binding in handlers)
			{
				if (binding.EventType == null)
				{
					continue;
				}

				var handlerDisplay = binding.HandlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				var eventDisplay = binding.EventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				var interfaceName = binding.Kind == EventHandlerKind.Async
					? "global::LayerBase.Core.EventHandler.IEventHandlerAsync"
					: "global::LayerBase.Core.EventHandler.IEventHandler";

				builder.Append("            typedLayer.Bind<").Append(eventDisplay).Append(">((")
					.Append(interfaceName).Append("<").Append(eventDisplay).Append(">)new ")
					.Append(handlerDisplay).AppendLine("());");
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
			return $"{sanitized}.LayerEventHandlers.g.cs";
		}

#pragma warning disable RS2008 // Enable analyzer release tracking
		private static class Diagnostics
		{
			private const string Category = "LayerEventHandlerGenerator";

			public static readonly DiagnosticDescriptor LayerMustInheritLayer =
				new DiagnosticDescriptor(
					id: "LBG102",
					title: "OwnerLayer target must derive from Layer",
					messageFormat: "Type '{0}' is not a Layer and cannot be used with OwnerLayerAttribute",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor LayerMustBePartial =
				new DiagnosticDescriptor(
					id: "LBG103",
					title: "Layer must be partial",
					messageFormat: "Layer '{0}' must be declared as partial to allow generator to emit registrations",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor HandlerNeedsPublicParameterlessConstructor =
				new DiagnosticDescriptor(
					id: "LBG104",
					title: "Event handler needs parameterless constructor",
					messageFormat: "Event handler '{0}' must have a public or internal parameterless constructor",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor HandlerCannotBeAbstract =
				new DiagnosticDescriptor(
					id: "LBG105",
					title: "Event handler cannot be abstract",
					messageFormat: "Event handler '{0}' cannot be abstract when used with OwnerLayerAttribute",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);
		}
#pragma warning restore RS2008

		private sealed class HandlerRegistration
		{
			public HandlerRegistration(INamedTypeSymbol handlerType, INamedTypeSymbol layerType, Location? location)
			{
				HandlerType = handlerType;
				LayerType = layerType;
				Location = location;
			}

			public INamedTypeSymbol HandlerType { get; }

			public INamedTypeSymbol LayerType { get; }

			public Location? Location { get; }
		}

		private sealed class HandlerBinding
		{
			public HandlerBinding(INamedTypeSymbol handlerType, INamedTypeSymbol layerType, ITypeSymbol? eventType, EventHandlerKind kind, Location? location)
			{
				HandlerType = handlerType;
				LayerType = layerType;
				EventType = eventType;
				Kind = kind;
				Location = location;
			}

			public INamedTypeSymbol HandlerType { get; }

			public INamedTypeSymbol LayerType { get; }

			public ITypeSymbol? EventType { get; }

			public EventHandlerKind Kind { get; }

			public Location? Location { get; }
		}

		private readonly record struct EventHandlerImplementation(ITypeSymbol EventType, EventHandlerKind Kind);

		private enum EventHandlerKind
		{
			Sync,
			Async
		}

		private sealed class HandlerBindingComparer : IEqualityComparer<HandlerBinding>
		{
			public static readonly HandlerBindingComparer Instance = new HandlerBindingComparer();

			public bool Equals(HandlerBinding? x, HandlerBinding? y)
			{
				if (ReferenceEquals(x, y))
				{
					return true;
				}

				if (x is null || y is null)
				{
					return false;
				}

				return SymbolEqualityComparer.Default.Equals(x.HandlerType, y.HandlerType)
				       && SymbolEqualityComparer.Default.Equals(x.EventType, y.EventType)
				       && x.Kind == y.Kind;
			}

			public int GetHashCode(HandlerBinding obj)
			{
				int hash = SymbolEqualityComparer.Default.GetHashCode(obj.HandlerType);
				hash = (hash * 397) ^ (obj.EventType != null ? SymbolEqualityComparer.Default.GetHashCode(obj.EventType) : 0);
				hash = (hash * 397) ^ (int)obj.Kind;
				return hash;
			}
		}
	}
}
