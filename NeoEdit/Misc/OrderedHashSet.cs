using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeoEdit.Program.Misc
{
	public class OrderedHashSet<T> : KeyedCollection<T, T>
	{
		public OrderedHashSet() { }

		public OrderedHashSet(IEnumerable<T> items) => items.ForEach(item => Add(item));

		protected override T GetKeyForItem(T item) => item;
	}
}
