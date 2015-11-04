using System.Collections.Generic;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExDataChar : RevRegExData
	{
		public char Value { get; }
		public RevRegExDataChar(char value) { Value = value; }
		public override List<string> GetPossibilities() => new List<string> { new string(Value, 1) };
		public override long Count() => 1;
		public override string ToString() => Value.ToString();
	}
}
