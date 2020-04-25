using System;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Editor.Content.CSharp;

namespace NeoEdit.Editor.Content
{
	static class NewParser
	{
		static public NewNode Parse(ParserType parserType, string input, bool strict)
		{
			switch (parserType)
			{
				case ParserType.CSharp: return CSharpWalker.Parse(input, strict);
				default: throw new Exception($"Don't know how to parse {parserType}");
			}
		}
	}
}
