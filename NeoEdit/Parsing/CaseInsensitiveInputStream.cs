using Antlr4.Runtime;

namespace NeoEdit.Program.Parsing
{
	public class CaseInsensitiveInputStream : AntlrInputStream
	{
		public CaseInsensitiveInputStream(string input) : base(input) { }

		public override int La(int i)
		{
			var value = base.La(i);
			if ((value >= char.MinValue) && (value <= char.MaxValue))
				value = char.ToLower((char)value);
			return value;
		}
	}
}
