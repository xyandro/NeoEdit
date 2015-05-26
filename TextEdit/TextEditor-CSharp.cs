using System.Linq;
using NeoEdit.Parsing;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		ParserNode CSharpRoot()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var data = GetString(allRange);
			return CSharp.Parse(data);
		}

		Range GetNodeRange(ParserNode node)
		{
			return new Range(node.LocationStart, node.LocationEnd);
		}

		internal void Command_CSharp_Methods()
		{
			Selections.Replace(CSharpRoot().List(ParserNode.ParserNodeListType.SelfAndDescendants).Where(node => node.Type == "Method").Select(node => GetNodeRange(node)).ToList());
		}
	}
}
