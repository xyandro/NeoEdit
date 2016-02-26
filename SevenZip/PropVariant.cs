using System;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	[StructLayout(LayoutKind.Explicit, Size = 24)]
	struct PropVariant
	{
		[DllImport("ole32.dll")]
		private static extern int PropVariantClear(ref PropVariant pvar);

		[FieldOffset(0)]
		public VarEnum VarType;
		[FieldOffset(8)]
		public int IntValue;
		[FieldOffset(8)]
		public long LongValue;
		[FieldOffset(8)]
		public IntPtr Value;

		public object GetObject()
		{
			if (VarType == VarEnum.VT_EMPTY)
				return null;
			if (VarType == VarEnum.VT_FILETIME)
				return DateTime.FromFileTime(LongValue);

			var pin = GCHandle.Alloc(this, GCHandleType.Pinned);
			try
			{
				return Marshal.GetObjectForNativeVariant(pin.AddrOfPinnedObject());
			}
			finally
			{
				pin.Free();
			}
		}

		public void Clear() => PropVariantClear(ref this);
	}
}
