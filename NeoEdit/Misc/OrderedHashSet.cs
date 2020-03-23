﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeoEdit.Common;

namespace NeoEdit.Program.Misc
{
	public class OrderedHashSet<T> : KeyedCollection<T, T>, IReadOnlyOrderedHashSet<T>
	{
		public OrderedHashSet() { }

		public OrderedHashSet(IEnumerable<T> items) => items.ForEach(item => Add(item));

		protected override T GetKeyForItem(T item) => item;
	}
}
