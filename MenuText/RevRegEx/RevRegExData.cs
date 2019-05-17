using System.Collections.Generic;

namespace NeoEdit.MenuText.RevRegEx
{
	abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
