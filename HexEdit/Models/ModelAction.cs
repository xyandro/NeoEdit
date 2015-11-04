using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Transform;

namespace NeoEdit.HexEdit.Models
{
	class ModelAction
	{
		public enum ActionType
		{
			None,
			Bit,
			BasicType,
			String,
			Unused,
			Model,
		}

		public enum ActionStringType
		{
			StringWithLength,
			StringNullTerminated,
			StringFixedWidth,
		}

		public enum ActionRepeatType
		{
			Times,
			DataPosition,
		}

		public ActionType Type { get; set; }
		public Coder.CodePage CodePage { get; set; } // For basic type: codepage; for varstring: length type
		public ActionStringType StringType { get; set; }
		public Coder.CodePage Encoding { get; set; }
		public string FixedWidth { get; set; } // For fixed-width string or unused
		public string Model { get; set; }
		public string Location { get; set; }
		public int AlignmentBits { get; set; }
		public ActionRepeatType RepeatType { get; set; }
		public string Repeat { get; set; }
		public string SaveName { get; set; }

		public string Description(IEnumerable<Model> models)
		{
			switch (Type)
			{
				case ActionType.Bit: return "Bit";
				case ActionType.BasicType: return CodePage.ToString();
				case ActionType.String:
					{
						var encoding = Coder.GetDescription(Encoding, true);
						switch (StringType)
						{
							case ActionStringType.StringWithLength: return $"Variable length string ({CodePage.ToString()} length, {encoding})";
							case ActionStringType.StringNullTerminated: return $"Null-terminated string ({encoding})";
							case ActionStringType.StringFixedWidth: return $"Fixed width string ({FixedWidth} bytes, {encoding})";
						}
					}
					break;
				case ActionType.Unused: return $"Unused ({FixedWidth} bytes)";
				case ActionType.Model:
					{
						var model = models.FirstOrDefault(a => a.GUID == Model);
						if (model == null)
							return "INVALID MODEL";
						return model.ModelName;
					}
			}
			return "Unknown";
		}

		public ModelAction()
		{
			Type = ActionType.None;
			CodePage = Coder.CodePage.Int32LE;
			StringType = ActionStringType.StringWithLength;
			Encoding = Coder.CodePage.UTF8;
			FixedWidth = "0";
			AlignmentBits = 8;
			RepeatType = ActionRepeatType.Times;
			Location = "Current";
			Repeat = "1";
			SaveName = null;
		}
	}
}
