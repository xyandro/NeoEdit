using System;

namespace NeoEdit.Program
{
	[Flags]
	public enum MessageOptions
	{
		None = 0,
		Yes = 1,
		No = 2,
		All = 4,
		Ok = 8,
		Cancel = 16,

		YesNo = Yes | No,
		YesNoAll = Yes | No | All,
		OkCancel = Ok | Cancel,
		YesNoCancel = Yes | No | Cancel,
		YesNoAllCancel = Yes | No | All | Cancel,
	}
}
