using System;
using System.Collections.Concurrent;

namespace LayerBase.Layers
{
	/// <summary>
	/// Stores per-layer DI registration actions that are filled by the source generator.
	/// </summary>
	public static class LayerServiceRegistry
	{
		private static readonly ConcurrentDictionary<Type, Action<Layer>> s_registrations = new();

		public static void Register(Type layerType, Action<Layer> registrar)
		{
			if (layerType == null) throw new ArgumentNullException(nameof(layerType));
			if (registrar == null) throw new ArgumentNullException(nameof(registrar));

			s_registrations.AddOrUpdate(layerType, registrar, (_, existing) => existing + registrar);
		}

		internal static void Apply(Layer layer)
		{
			if (layer == null) throw new ArgumentNullException(nameof(layer));
			if (s_registrations.TryGetValue(layer.GetType(), out var registrar))
			{
				registrar(layer);
			}
		}
	}
}
