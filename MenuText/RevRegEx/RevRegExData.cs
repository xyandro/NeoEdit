using System.Collections.Generic;

namespace NeoEdit.RevRegEx
{
	abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
