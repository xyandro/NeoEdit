using System.Collections.Generic;

namespace NeoEdit.TextEdit.RevRegEx
{
	abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
