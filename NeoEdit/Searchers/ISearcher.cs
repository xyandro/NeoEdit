using System.Collections.Generic;

namespace NeoEdit.Program.Searchers
{
	public interface ISearcher
	{
		List<Range> Find(string input, int addOffset = 0);
	}
}
