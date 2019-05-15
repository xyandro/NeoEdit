using System;

namespace NeoEdit.Parsing
{
	public class ParserException : Exception
	{
		public int Line { get; }
		public int Pos { get; }
		public string Msg { get; }

		public ParserException(int line, int pos, string msg, Exception innerException) : base($"Failed to parse at line {line} pos {pos}: {msg}", innerException)
		{
			Line = line;
			Pos = pos;
			Msg = msg;
		}
	}
}
