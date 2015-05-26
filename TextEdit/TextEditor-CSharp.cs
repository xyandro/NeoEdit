using System.Linq;
using NeoEdit.Parsing;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		CSharpNode CSharpRoot()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			return CSharp.Parse(data);
		}

		Range GetNodeRange(CSharpNode node)
		{
			var location = node.Location;
			return new Range(location.Item1, location.Item2);
		}

		internal void Command_CSharp_Methods()
		{
			Selections.Replace(CSharpRoot().List(ParserNodeListType.SelfAndDescendants).Cast<CSharpNode>().Where(node => node.NodeType == CSharpNodeType.Method).Select(node => GetNodeRange(node)).ToList());
		}
	}
}
