using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.Content
{
	public class NewNode
	{
		public Range Range { get; }
		public NewNode Parent { get; set; }
		public List<NewNode> Children { get; } = new List<NewNode>();

		public NewNode(Range range)
		{
			Range = range;
		}
	}
}
