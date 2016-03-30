namespace NeoEdit.Console
{
	class Line
	{
		public enum LineType
		{
			Command,
			StdOut,
			StdErr,
		}

		public string Str { get; }
		public LineType Type { get; }
		public bool Finished { get; }

		public Line(LineType Type) : this(string.Empty, Type, false) { }
		public Line(string Str, LineType Type) : this(Str, Type, false) { }

		Line(string Str, LineType Type, bool Finished)
		{
			this.Str = Str;
			this.Type = Type;
			this.Finished = Finished;
		}

		static public Line operator +(Line line, string str)
		{
			if (string.IsNullOrEmpty(str))
				return line;
			return new Line(line.Str + str, line.Type, line.Finished);
		}

		public Line Finish()
		{
			if (Finished)
				return this;
			return new Line(Str, Type, true);
		}
	}
}
