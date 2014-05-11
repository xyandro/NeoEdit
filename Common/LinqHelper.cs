using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	class LinqHelperComparer<TKey> : IComparer<TKey>
	{
		Func<TKey, TKey, int> func;
		public LinqHelperComparer(Func<TKey, TKey, int> _func)
		{
			func = _func;
		}

		public int Compare(TKey a, TKey b)
		{
			return func(a, b);
		}
	}

	public static class LinqHelper
	{
		public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> comparer)
		{
			return source.OrderBy(keySelector, new LinqHelperComparer<TKey>(comparer));
		}
	}
}
