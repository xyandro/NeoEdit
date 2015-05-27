using System;
using System.IO;

namespace NeoEdit.Parsing
{
	static public class Parser
	{
		public enum ParserType
		{
			None,
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

		static public ParserType GetParserType(string fileName)
		{
			if (String.IsNullOrEmpty(fileName))
				return ParserType.None;
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".htm":
				case ".html": return ParserType.HTML;
				case ".cs": return ParserType.CSharp;
				default: return ParserType.None;
			}
		}
	}
}
