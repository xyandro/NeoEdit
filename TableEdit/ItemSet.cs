﻿using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.TableEdit
{
	class ItemSet<T> : List<T>
	{
		public ItemSet(IEnumerable<T> items) : base(items) { }

		public override bool Equals(object obj)
		{
			if (obj is ItemSet<T>)
				return Equals(obj as ItemSet<T>);
			return false;
		}

		public bool Equals(ItemSet<T> items)
		{
			return this.Matches(items);
		}

		public override int GetHashCode()
		{
			var code = 0;
			foreach (var item in this)
				code += item.GetHashCode();
			return code;
		}
	}

	static class ItemSetExtensions
	{
		public static ItemSet<TSource> ToItemSet<TSource>(this IEnumerable<TSource> source)
		{
			return new ItemSet<TSource>(source);
		}
	}
}
