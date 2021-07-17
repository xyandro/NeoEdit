using System;

namespace NeoEdit.Common.Expressions
{
	public class NEVariableUse
	{
		public string Name { get; }
		public string Display => ToString();
		public NEVariableRepeat Repeat { get; }

		public NEVariableUse(string name, NEVariableRepeat repeat)
		{
			Name = name;
			Repeat = repeat;
		}

		public NEVariableUse(string name, string repeat) : this(name, GetRepeat(repeat)) { }

		static NEVariableRepeat GetRepeat(string text)
		{
			var isCycle = text.IndexOf('@') != -1;
			var isRepeat = text.IndexOf('#') != -1;
			if (isCycle)
			{
				if (isRepeat)
					return NEVariableRepeat.None;
				return NEVariableRepeat.Cycle;
			}
			if (isRepeat)
				return NEVariableRepeat.Repeat;
			return NEVariableRepeat.None;
		}

		public override string ToString()
		{
			string repeat;
			switch (Repeat)
			{
				case NEVariableRepeat.None: repeat = ""; break;
				case NEVariableRepeat.Cycle: repeat = "@"; break;
				case NEVariableRepeat.Repeat: repeat = "#"; break;
				default: throw new Exception($"Invalid {nameof(Repeat)}");
			}
			return $"{repeat}{Name}";
		}
	}
}
