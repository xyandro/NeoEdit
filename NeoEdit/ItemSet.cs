using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Program
{
	public class ItemSet<T> : List<T>
	{
		public ItemSet() { }
		public ItemSet(IEnumerable<T> items) : base(items) { }

		public override bool Equals(object obj)
		{
			if (obj is ItemSet<T>)
				return Equals(obj as ItemSet<T>);
			return false;
		}

		public bool Equals(ItemSet<T> items) => this.Matches(items);

		public override int GetHashCode()
		{
			var code = 0;
			foreach (var item in this)
				code ^= item == null ? 0x0badf00d : item.GetHashCode();
			return code;
		}
	}

	public static class ItemSetExtensions
	{
		public static ItemSet<TSource> ToItemSet<TSource>(this IEnumerable<TSource> source) => new ItemSet<TSource>(source);
	}
}
