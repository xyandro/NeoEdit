using System.Collections.Generic;

namespace NeoEdit.Common
{
	public interface IReadOnlyOrderedHashSet<T> : IReadOnlyList<T>, IEnumerable<T>
	{
		int IndexOf(T item);
	}
}
