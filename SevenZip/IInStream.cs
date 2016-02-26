using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[ComImport]
	[Guid("23170F69-40C1-278A-0000-000300030000")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IInStream
	{
		int Read([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data, int size);
		void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition);
	}
}
