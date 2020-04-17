using Antlr4.Runtime;

namespace NeoEdit.Common.Parsing
{
	public class CaseInsensitiveInputStream : AntlrInputStream
	{
		public CaseInsensitiveInputStream(string input) : base(input) { }

		public override int LA(int i)
		{
			var value = base.LA(i);
			if ((value >= char.MinValue) && (value <= char.MaxValue))
				value = char.ToLower((char)value);
			return value;
		}
	}
}
