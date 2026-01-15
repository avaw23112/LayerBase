using System;

namespace LayerBase.Core.EventCatalogue
{
	/// <summary>
	/// 事件分类的标识符。等价于类型令牌，保证每个分类都有独立Id。
	/// </summary>
	public readonly struct EventCategoryToken : IEquatable<EventCategoryToken>
	{
		public int Id { get; }
		public string Path { get; } = string.Empty;
		public bool IsValid => Id > 0;

		internal EventCategoryToken(int id, string path)
		{
			Id = id;
			Path = path;
		}

		public override string ToString() => Path;

		public bool Equals(EventCategoryToken other) => Id == other.Id;
		public override bool Equals(object? obj) => obj is EventCategoryToken other && Equals(other);
		public override int GetHashCode() => Id;
		public static bool operator ==(EventCategoryToken left, EventCategoryToken right) => left.Equals(right);
		public static bool operator !=(EventCategoryToken left, EventCategoryToken right) => !left.Equals(right);
	}
}
