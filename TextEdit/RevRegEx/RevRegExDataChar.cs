using System.Collections.Generic;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExDataChar : RevRegExData
	{
		public char Value { get; private set; }
		public RevRegExDataChar(char value) { Value = value; }
		public override List<string> GetPossibilities() { return new List<string> { new string(Value, 1) }; }
		public override long Count() { return 1; }
		public override string ToString() { return Value.ToString(); }
	}
}
