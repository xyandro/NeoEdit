using System;

namespace Loader
{
	[Flags]
	public enum BitDepths
	{
		None = 0,
		x32 = 1,
		x64 = 2,
		Any = x32 | x64,
	}
}
