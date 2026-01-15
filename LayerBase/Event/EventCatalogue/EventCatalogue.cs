using System;
using System.Collections.Generic;
using System.Threading;

namespace LayerBase.Core.EventCatalogue
{
	/// <summary>
	/// 事件分类目录：支持树状结构、运行时动态扩展，以及类型到分类的绑定。
	/// </summary>
	public sealed class EventCatalogue
	{
		private readonly CategoryNode m_root;
		private readonly Dictionary<int, CategoryNode> m_nodesById = new();
		private readonly Dictionary<Type, EventCategoryToken> m_typeBindings = new();
		private readonly ReaderWriterLockSlim m_lock = new();
		private readonly StringComparer m_nameComparer;
		private int m_nextId;

		/// <param name="rootName">根节点名字，仅用于展示，不参与路径计算。</param>
		/// <param name="comparer">分类名比较方式，默认不区分大小写。</param>
		public EventCatalogue(string rootName = "root", StringComparer? comparer = null)
		{
			if (rootName == null) throw new ArgumentNullException(nameof(rootName));

			m_nameComparer = comparer ?? StringComparer.OrdinalIgnoreCase;
			m_root = new CategoryNode(0, rootName.Trim(), null, m_nameComparer, treatAsRoot: true);
			m_nodesById[0] = m_root;
			m_nextId = 0;
		}

		/// <summary>
		/// 确保分类路径存在，不存在则创建。支持“Game/Combat/Skill”形式。
		/// </summary>
		public EventCategoryToken Ensure(string categoryPath)
		{
			if (string.IsNullOrWhiteSpace(categoryPath))
				throw new ArgumentException("分类路径不能为空", nameof(categoryPath));

			string[] segments = categoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (segments.Length == 0)
				throw new ArgumentException("分类路径不能为空", nameof(categoryPath));

			m_lock.EnterWriteLock();
			try
			{
				CategoryNode node = m_root;
				for (int i = 0; i < segments.Length; i++)
				{
					string segment = segments[i];
					if (!node.TryGetChild(segment, out var child))
					{
						child = node.AddChild(NextId(), segment, m_nameComparer);
						m_nodesById[child.Id] = child;
					}
					node = child;
				}

				return node.ToToken();
			}
			finally
			{
				m_lock.ExitWriteLock();
			}
		}

		/// <summary>
		/// 使用枚举值创建分类，达到“像用枚举命名”但仍保持动态的体验。
		/// </summary>
		public EventCategoryToken Ensure<TEnum>(TEnum category, string? parentPath = null) where TEnum : struct, Enum
		{
			string name = category.ToString();
			string path = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath}/{name}";
			return Ensure(path);
		}

		/// <summary>
		/// 尝试获取已存在的分类，不创建新节点。
		/// </summary>
		public bool TryGet(string categoryPath, out EventCategoryToken token)
		{
			token = default;
			if (string.IsNullOrWhiteSpace(categoryPath))
				return false;

			string[] segments = categoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (segments.Length == 0)
				return false;

			m_lock.EnterReadLock();
			try
			{
				CategoryNode node = m_root;
				for (int i = 0; i < segments.Length; i++)
				{
					string segment = segments[i];
					if (!node.TryGetChild(segment, out var child))
					{
						return false;
					}
					node = child;
				}

				token = node.ToToken();
				return true;
			}
			finally
			{
				m_lock.ExitReadLock();
			}
		}

		/// <summary>
		/// 使用枚举名尝试获取分类，不创建新节点。
		/// </summary>
		public bool TryGet<TEnum>(TEnum category, out EventCategoryToken token) where TEnum : struct, Enum
		{
			string name = category.ToString();
			return TryGet(name, out token);
		}

		/// <summary>
		/// 将事件参数类型绑定到某个分类，后续可以直接按类型校验归属。
		/// </summary>
		public EventCategoryToken BindEvent<TEventArg>(string categoryPath) where TEventArg : struct
		{
			EventCategoryToken token = Ensure(categoryPath);
			BindType(typeof(TEventArg), token);
			return token;
		}

		/// <summary>
		/// 将事件参数类型绑定到已有分类令牌。
		/// </summary>
		public EventCategoryToken BindEvent<TEventArg>(EventCategoryToken categoryToken) where TEventArg : struct
		{
			if (!categoryToken.IsValid)
			{
				throw new ArgumentException("无效分类Token", nameof(categoryToken));
			}

			EnsureTokenFromThisCatalogue(categoryToken);
			BindType(typeof(TEventArg), categoryToken);
			return categoryToken;
		}

		/// <summary>
		/// 结合枚举定义绑定事件类型到分类。
		/// </summary>
		public EventCategoryToken BindEvent<TEventArg, TEnum>(TEnum category, string? parentPath = null) where TEventArg : struct where TEnum : struct, Enum
		{
			EventCategoryToken token = Ensure(category, parentPath);
			BindType(typeof(TEventArg), token);
			return token;
		}

		private void BindType(Type type, EventCategoryToken token)
		{
			m_lock.EnterWriteLock();
			try
			{
				m_typeBindings[type] = token;
			}
			finally
			{
				m_lock.ExitWriteLock();
			}
		}

		/// <summary>
		/// 获取某个事件类型绑定的分类。
		/// </summary>
		public bool TryGetTokenFor<TEventArg>(out EventCategoryToken token) where TEventArg : struct
		{
			m_lock.EnterReadLock();
			try
			{
				return m_typeBindings.TryGetValue(typeof(TEventArg), out token);
			}
			finally
			{
				m_lock.ExitReadLock();
			}
		}

		/// <summary>
		/// 判断某事件类型是否属于指定分类（包括父级）。
		/// </summary>
		public bool Is<TEventArg>(EventCategoryToken expectedCategory) where TEventArg : struct
		{
			if (!TryGetTokenFor<TEventArg>(out var current))
			{
				return false;
			}
			return IsInCategory(current, expectedCategory);
		}

		/// <summary>
		/// 判断某事件类型是否属于指定分类（通过路径，路径不存在时返回false）。
		/// </summary>
		public bool Is<TEventArg>(string expectedCategoryPath) where TEventArg : struct
		{
			if (!TryGet(expectedCategoryPath, out var expected))
			{
				return false;
			}
			return Is<TEventArg>(expected);
		}

		/// <summary>
		/// 判断某分类是否位于目标分类（自身或祖先）之下。
		/// </summary>
		public bool IsInCategory(EventCategoryToken value, EventCategoryToken expected)
		{
			if (!value.IsValid || !expected.IsValid)
			{
				return false;
			}

			m_lock.EnterReadLock();
			try
			{
				if (!m_nodesById.TryGetValue(value.Id, out var node))
				{
					return false;
				}

				int expectedId = expected.Id;
				CategoryNode? cursor = node;
				while (cursor != null)
				{
					if (cursor.Id == expectedId)
					{
						return true;
					}
					cursor = cursor.Parent;
				}
				return false;
			}
			finally
			{
				m_lock.ExitReadLock();
			}
		}

		private void EnsureTokenFromThisCatalogue(EventCategoryToken token)
		{
			m_lock.EnterReadLock();
			try
			{
				if (!m_nodesById.ContainsKey(token.Id))
				{
					throw new InvalidOperationException("分类Token不属于当前目录，无法绑定。");
				}
			}
			finally
			{
				m_lock.ExitReadLock();
			}
		}

		private int NextId() => Interlocked.Increment(ref m_nextId);

		private sealed class CategoryNode
		{
			private readonly Dictionary<string, CategoryNode> m_children;
			internal CategoryNode(int id, string name, CategoryNode? parent, StringComparer comparer, bool treatAsRoot = false)
			{
				Id = id;
				Name = name;
				Parent = parent;
				Path = treatAsRoot
					? string.Empty
					: parent == null || string.IsNullOrEmpty(parent.Path)
						? name
						: $"{parent.Path}/{name}";

				m_children = new Dictionary<string, CategoryNode>(comparer);
			}

			internal int Id { get; }
			internal string Name { get; }
			internal string Path { get; }
			internal CategoryNode? Parent { get; }

			internal bool TryGetChild(string name, out CategoryNode node) => m_children.TryGetValue(name, out node);

			internal CategoryNode AddChild(int id, string name, StringComparer comparer)
			{
				CategoryNode child = new CategoryNode(id, name, this, comparer);
				m_children[name] = child;
				return child;
			}

			internal EventCategoryToken ToToken() => new EventCategoryToken(Id, Path);
		}
	}
}
