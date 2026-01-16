namespace LayerBase.Core.Event
{
	/// <summary>
	/// 记录不同Event的独立签名
	/// </summary>
	internal static class EventTypeIdProvider
	{
		private static int s_nextId = 0;
		private static readonly Dictionary<Type, int> s_typeToId = new();
		private static readonly Dictionary<int, Type> s_IdToType = new();
		private static readonly object s_lock = new object();

		public static int GetOrCreateId(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			lock (s_lock)
			{
				if (s_typeToId.TryGetValue(type, out int id))
				{
					return id;
				}
				id = Interlocked.Increment(ref s_nextId);
				s_typeToId[type] = id;
				s_IdToType[id] = type;
				return id;
			}
		}
		
		public static Type GetType(int id)
		{
			if (s_IdToType.TryGetValue(id, out Type type))
			{
				return type;
			}
			return null;
		}
	}

	internal class EventTypeId<Value>
	{
		public static readonly int Id = EventTypeIdProvider.GetOrCreateId(typeof(Value));
	}
	internal class EventTypeId
	{
		public static int GetId(Type type) => EventTypeIdProvider.GetOrCreateId(type);
		public static Type GetType(int id) => EventTypeIdProvider.GetType(id);
	}
}