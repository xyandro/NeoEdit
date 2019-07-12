using System;

namespace NeoEdit.Program
{
	[Flags]
	public enum TimestampType
	{
		Write = 1,
		Access = 2,
		Create = 4,
		All = Write | Access | Create,
	}
}
