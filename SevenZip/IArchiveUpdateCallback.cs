using System;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[ComImport]
	[Guid("23170F69-40C1-278A-0000-000600800000")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IArchiveUpdateCallback
	{
		void SetTotal(ulong total);
		void SetCompleted([In] ref ulong completeValue);
		[PreserveSig]
		int GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive);
		[PreserveSig]
		int GetProperty(uint index, PropID propId, ref PropVariant value);
		[PreserveSig]
		int GetStream(uint index, [Out, MarshalAs(UnmanagedType.Interface)] out ISequentialInStream inStream);
		void SetOperationResult(OperationResult operationResult);
		long EnumProperties(IntPtr enumerator);
	}
}
