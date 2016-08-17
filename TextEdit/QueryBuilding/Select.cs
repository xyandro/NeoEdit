using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit.QueryBuilding
{
	abstract class Select
	{
		public abstract IEnumerable<string> Columns { get; }
		public abstract IEnumerable<string> QueryLines { get; }
		public virtual string Query => string.Join(" ", QueryLines.Select(str => str.Trim()));
	}
}
