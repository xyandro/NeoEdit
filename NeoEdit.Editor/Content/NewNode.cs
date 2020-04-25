using System.Collections.Generic;
using System.Xml.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor.Content
{
	public class NewNode
	{
		public string Type { get; internal set; }
		public Range Range { get; }
		public NewNode Parent { get; set; }
		public List<NewNode> Children { get; } = new List<NewNode>();

		public NewNode(string type, Range range)
		{
			Type = type;
			Range = range;
		}

		public XElement ToXML()
		{
			var xml = new XElement(Type);
			xml.Add(new XAttribute("Range", $"{Range.Start}-{Range.End}"));
			foreach (var child in Children)
				xml.Add(child.ToXML());
			return xml;
		}

		public override string ToString() => $"{Type}: {Range}";
	}
}
