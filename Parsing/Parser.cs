using System;

namespace NeoEdit.Parsing
{
	static public class Parser
	{
		public enum ParserType
		{
			CSharp,
			HTML,
		}

		static public ParserNode Parse(string data, ParserType parserType)
		{
			switch (parserType)
			{
				case ParserType.CSharp: return CSharp.Parse(data);
				case ParserType.HTML: return new HTML(data).Parse();
				default: throw new ArgumentException("Invalid parser value");
			}
		}
	}
}
