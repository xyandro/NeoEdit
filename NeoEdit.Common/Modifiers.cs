using System;

namespace NeoEdit.Common
{
	[Flags]
	public enum Modifiers
	{
		None = 0,
		Alt = 1,
		Control = 2,
		Shift = 4,
	}
}
