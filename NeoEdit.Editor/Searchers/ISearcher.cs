using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.Searchers
{
	public interface ISearcher
	{
		List<NERange> Find(string input, int addOffset = 0);
	}
}
