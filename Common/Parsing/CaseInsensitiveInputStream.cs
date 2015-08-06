using System;
using Antlr4.Runtime;

namespace NeoEdit.Common.Parsing
{
	public class CaseInsensitiveInputStream : AntlrInputStream
	{
		public CaseInsensitiveInputStream(string input) : base(input) { }

		public override int La(int i)
		{
			var value = base.La(i);
			if ((value >= Char.MinValue) && (value <= Char.MaxValue))
				value = Char.ToLower((char)value);
			return value;
		}
	}
}
