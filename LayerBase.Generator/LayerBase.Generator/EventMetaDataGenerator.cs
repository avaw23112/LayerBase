using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator
{
	[Generator(LanguageNames.CSharp)]
	public sealed class EventMetaDataGenerator : IIncrementalGenerator
	{
		private const string EventMetaDataBaseName = "LayerBase.Event.EventMetaData.EventMetaData`1";
		private const string EventMetaDataRegistryName = "LayerBase.Event.EventMetaData.EventMetaDataRegistry";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var registrations = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: { } },
					transform: static (ctx, ct) => CreateRegistration(ctx, ct))
				.Where(static r => r is not null)
				.Select(static (r, _) => r!);

			var compilationAndRegistrations = context.CompilationProvider.Combine(registrations.Collect());

			context.RegisterSourceOutput(compilationAndRegistrations, static (spc, source) =>
			{
				var compilation = source.Left;
				var collected = source.Right;

				var eventMetaDataBase = compilation.GetTypeByMetadataName(EventMetaDataBaseName);
				var registryType = compilation.GetTypeByMetadataName(EventMetaDataRegistryName);

				if (eventMetaDataBase == null || registryType == null)
				{
					return;
				}

				List<MetaDataRegistration> validRegistrations = new();
				HashSet<INamedTypeSymbol> registeredEvents = new(SymbolEqualityComparer.Default);

				foreach (var registration in collected)
				{
					foreach (var diagnostic in registration.Diagnostics)
					{
						spc.ReportDiagnostic(diagnostic);
					}

					if (!registration.IsValid)
					{
						continue;
					}

					if (registration.EventType is not INamedTypeSymbol eventTypeSymbol)
					{
						continue;
					}

					if (!IsPartial(eventTypeSymbol))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.EventTypeMustBePartial, registration.MetaDataType.Locations.FirstOrDefault(), eventTypeSymbol.ToDisplayString()));
						continue;
					}

					if (!registeredEvents.Add(eventTypeSymbol))
					{
						spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.DuplicateMetaDataForEventType, registration.MetaDataType.Locations.FirstOrDefault(), eventTypeSymbol.ToDisplayString()));
						continue;
					}

					validRegistrations.Add(registration);
				}

				if (validRegistrations.Count == 0)
				{
					return;
				}

				var sourceText = GenerateSource(validRegistrations, compilation.AssemblyName ?? "Assembly");
				if (!string.IsNullOrEmpty(sourceText))
				{
					spc.AddSource(CreateHintName(compilation.AssemblyName), SourceText.From(sourceText, Encoding.UTF8));
				}
			});
		}

		private static MetaDataRegistration? CreateRegistration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) as INamedTypeSymbol;
			if (typeSymbol == null)
			{
				return null;
			}

			var eventMetaDataSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(EventMetaDataBaseName);
			if (eventMetaDataSymbol == null)
			{
				return null;
			}

			var eventType = GetEventType(typeSymbol, eventMetaDataSymbol);
			if (eventType == null)
			{
				return null;
			}

			var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
			var location = typeSymbol.Locations.FirstOrDefault();

			if (typeSymbol.IsAbstract)
			{
				diagnostics.Add(Diagnostic.Create(Diagnostics.MetaDataCannotBeAbstract, location, typeSymbol.ToDisplayString()));
			}

			if (typeSymbol.TypeParameters.Length > 0)
			{
				diagnostics.Add(Diagnostic.Create(Diagnostics.MetaDataCannotBeGeneric, location, typeSymbol.ToDisplayString()));
			}

			if (!HasAccessibleParameterlessConstructor(typeSymbol))
			{
				diagnostics.Add(Diagnostic.Create(Diagnostics.MetaDataNeedsPublicParameterlessConstructor, location, typeSymbol.ToDisplayString()));
			}

			return new MetaDataRegistration(typeSymbol, eventType, diagnostics.ToImmutable());
		}

		private static INamedTypeSymbol? GetEventType(INamedTypeSymbol type, INamedTypeSymbol eventMetaDataSymbol)
		{
			for (var current = type; current != null; current = current.BaseType)
			{
				if (current is INamedTypeSymbol named &&
				    named.IsGenericType &&
				    SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, eventMetaDataSymbol))
				{
					return named.TypeArguments[0] as INamedTypeSymbol;
				}
			}

			return null;
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

		private static string GenerateSource(IEnumerable<MetaDataRegistration> registrations, string assemblyName)
		{
			_ = assemblyName;
			var grouped = registrations
				.Distinct(MetaDataRegistrationComparer.Instance)
				.Where(r => r.EventType is not null)
				.GroupBy(r => r.EventType!, SymbolEqualityComparer.Default)
				.OrderBy(g => g.Key?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
				.ToList();

			if (grouped.Count == 0)
			{
				return string.Empty;
			}

			var builder = new StringBuilder();
			builder.AppendLine("// <auto-generated />");
			builder.AppendLine("// This file was generated by EventMetaDataGenerator.");
			foreach (var group in grouped)
			{
				if (group.Key is not INamedTypeSymbol eventType)
				{
					continue;
				}
				var metaData = group.First();

				var metaDataDisplay = metaData.MetaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				var eventTypeDisplay = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				var eventIdentifier = eventType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
				var @namespace = eventType.ContainingNamespace is { IsGlobalNamespace: false } ns
					? ns.ToDisplayString()
					: null;
				var typeKeyword = eventType.TypeKind == TypeKind.Struct ? "struct" : "class";

				if (!string.IsNullOrEmpty(@namespace))
				{
					builder.Append("namespace ").Append(@namespace).AppendLine();
					builder.AppendLine("{");
				}

				builder.Append("partial ").Append(typeKeyword).Append(' ').Append(eventIdentifier).AppendLine();
				builder.AppendLine("{");
				builder.Append("    static ").Append(eventType.Name).AppendLine("()");
				builder.AppendLine("    {");
				builder.Append("        global::LayerBase.Event.EventMetaData.EventMetaDataRegistry.RegisterMetaData<")
					.Append(eventTypeDisplay)
					.Append(">(new ")
					.Append(metaDataDisplay)
					.AppendLine("());");
				builder.AppendLine("    }");
				builder.AppendLine("}");

				if (!string.IsNullOrEmpty(@namespace))
				{
					builder.AppendLine("}");
				}

				builder.AppendLine();
			}

			return builder.ToString();
		}

		private static string CreateHintName(string? assemblyName)
		{
			var sanitized = Sanitize(string.IsNullOrWhiteSpace(assemblyName) ? "Assembly" : assemblyName!);
			return $"{sanitized}.EventMetaData.g.cs";
		}

		private static string Sanitize(string value)
		{
			var sanitized = new StringBuilder(value.Length);
			foreach (var ch in value)
			{
				sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
			}

			if (sanitized.Length == 0)
			{
				sanitized.Append("Assembly");
			}

			return sanitized.ToString();
		}

		private sealed record MetaDataRegistration(INamedTypeSymbol MetaDataType, INamedTypeSymbol EventType, ImmutableArray<Diagnostic> Diagnostics)
		{
			public bool IsValid => Diagnostics.IsDefaultOrEmpty;
		}

		private sealed class MetaDataRegistrationComparer : IEqualityComparer<MetaDataRegistration>
		{
			public static readonly MetaDataRegistrationComparer Instance = new();

			public bool Equals(MetaDataRegistration x, MetaDataRegistration y)
			{
				return SymbolEqualityComparer.Default.Equals(x.MetaDataType, y.MetaDataType);
			}

			public int GetHashCode(MetaDataRegistration obj)
			{
				return SymbolEqualityComparer.Default.GetHashCode(obj.MetaDataType);
			}
		}

#pragma warning disable RS2008 // Enable analyzer release tracking
		private static class Diagnostics
		{
			private const string Category = "EventMetaDataGenerator";

			public static readonly DiagnosticDescriptor MetaDataNeedsPublicParameterlessConstructor =
				new DiagnosticDescriptor(
					id: "LBG204",
					title: "Event metadata needs parameterless constructor",
					messageFormat: "Event metadata '{0}' must have a public or internal parameterless constructor",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor MetaDataCannotBeAbstract =
				new DiagnosticDescriptor(
					id: "LBG205",
					title: "Event metadata cannot be abstract",
					messageFormat: "Event metadata '{0}' cannot be abstract",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor MetaDataCannotBeGeneric =
				new DiagnosticDescriptor(
					id: "LBG206",
					title: "Event metadata cannot be generic",
					messageFormat: "Event metadata '{0}' cannot be generic when used for registration",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor EventTypeMustBePartial =
				new DiagnosticDescriptor(
					id: "LBG207",
					title: "Event type must be partial",
					messageFormat: "Event type '{0}' must be partial to allow metadata registration",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);

			public static readonly DiagnosticDescriptor DuplicateMetaDataForEventType =
				new DiagnosticDescriptor(
					id: "LBG208",
					title: "Duplicate event metadata registration",
					messageFormat: "Only one EventMetaData can target event type '{0}'",
					category: Category,
					defaultSeverity: DiagnosticSeverity.Error,
					isEnabledByDefault: true);
		}
#pragma warning restore RS2008
	}
}
