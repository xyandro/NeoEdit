using System.Collections.Generic;

namespace NeoEdit.Editor.RevRegEx
{
	public abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
