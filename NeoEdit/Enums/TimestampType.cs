using System;

namespace NeoEdit
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
