using System;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[ComImport]
	[Guid("23170F69-40C1-278A-0000-000600200000")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IArchiveExtractCallback
	{
		void SetTotal(ulong total);
		void SetCompleted([In] ref ulong completeValue);
		[PreserveSig]
		int GetStream(int index, [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream outStream, AskMode askExtractMode);
		void PrepareOperation(AskMode askExtractMode);
		void SetOperationResult(OperationResult resultEOperationResult);
	}
}
