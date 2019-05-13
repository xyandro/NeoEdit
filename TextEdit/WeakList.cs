using System;
using System.Collections;
using System.Collections.Generic;

namespace NeoEdit.TextEdit
{
	public class WeakList<T> : IEnumerable<T> where T : class
	{
		List<WeakReference<T>> list = new List<WeakReference<T>>();

		public void Add(T item) => list.Add(new WeakReference<T>(item));

		public IEnumerator<T> GetEnumerator()
		{
			var remove = new HashSet<WeakReference<T>>();
			foreach (var item in list)
			{
				T target;
				if (item.TryGetTarget(out target))
					yield return target;
				else
					remove.Add(item);
			}
			list.RemoveAll(item => remove.Contains(item));
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
