using System;

namespace NeoEdit.Program
{
	[Flags]
	public enum MessageOptions
	{
		None = 0,
		Yes = 1,
		No = 2,
		YesNo = Yes | No,
		YesToAll = 4,
		YesNoYesAll = Yes | No | YesToAll,
		NoToAll = 8,
		YesNoNoAll = Yes | No | NoToAll,
		YesNoYesAllNoAll = Yes | No | YesToAll | NoToAll,
		Ok = 16,
		Cancel = 32,
		OkCancel = Ok | Cancel,
		YesNoCancel = Yes | No | Cancel,
		YesNoYesAllNoAllCancel = Yes | No | YesToAll | NoToAll | Cancel,
	}
}
