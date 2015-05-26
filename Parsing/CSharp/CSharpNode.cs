namespace NeoEdit
{
	namespace Parsing
	{
		public enum CSharpNodeType
		{
			Root,
			Class,
			Method,
		}

		public class CSharpNode : ParserNode<CSharpNodeType>
		{
		}
	}
}
