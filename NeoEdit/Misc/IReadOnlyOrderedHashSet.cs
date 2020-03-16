using System.Collections.Generic;

namespace NeoEdit.Program.Misc
{
	public interface IReadOnlyOrderedHashSet<T> : IReadOnlyList<T>, IEnumerable<T>
	{
		int IndexOf(T item);
	}
}
