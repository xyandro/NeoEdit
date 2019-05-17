using System;

namespace NeoEdit.Loader
{
	[Flags]
	public enum FileTypes
	{
		None = 0,
		Native = 1,
		Managed = 2,
		Mixed = Native | Managed,
	}
}
