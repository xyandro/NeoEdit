using System;

namespace NeoEdit.HexEdit.Models
{
	class ModelResult
	{
		public ModelAction Action { get; private set; }
		public string Value { get; private set; }
		public long StartByte { get; private set; }
		public int StartBit { get; private set; }
		public long EndByte { get; private set; }
		public int EndBit { get; private set; }

		public ModelResult(ModelAction action, string value, long startByte, int startBit, long endByte, int endBit)
		{
			Action = action;
			Value = value;
			StartByte = startByte;
			StartBit = startBit;
			EndByte = endByte;
			EndBit = endBit;
		}

		public override string ToString()
		{
			return String.Format("{0}: {1}.{2} - {3}.{4}", Value, StartByte, StartBit, EndByte, EndBit);
		}
	}
}
