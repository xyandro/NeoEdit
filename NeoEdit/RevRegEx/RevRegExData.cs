using System.Collections.Generic;

namespace NeoEdit.Program.RevRegEx
{
	abstract class RevRegExData
	{
		public abstract List<string> GetPossibilities();
		public abstract long Count();
	}
}
