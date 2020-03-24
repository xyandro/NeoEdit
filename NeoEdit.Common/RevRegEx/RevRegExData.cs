using System.Collections.Generic;

namespace NeoEdit.Common.RevRegEx
{
	public abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
