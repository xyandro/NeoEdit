using System;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[ComImport]
	[Guid("23170F69-40C1-278A-0000-000600600000")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IInArchive
	{
		int Open(IInStream stream, [In] ref ulong maxCheckStartPosition, IntPtr openArchiveCallback);
		void Close();
		uint GetNumberOfItems();
		void GetProperty(uint index, PropID propID, ref PropVariant value);
		[PreserveSig]
		int Extract([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indices, uint numItems, int testMode, [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);
	}
}
