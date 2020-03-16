using System.Collections.Generic;

namespace NeoEdit.Program.Misc
{
	public interface IReadOnlyOrderedHashSet<T> : IEnumerable<T>
	{
		int Count { get; }
		T this[int index] { get; }
		int IndexOf(T item);
	}
}
