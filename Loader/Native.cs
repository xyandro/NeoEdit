using System;
using System.Runtime.InteropServices;

namespace Loader
{
	static class Native
	{
		public static readonly IntPtr RT_ICON = (IntPtr)3;
		public static readonly IntPtr RT_GROUP_ICON = (IntPtr)14;
		public static readonly IntPtr RT_VERSION = (IntPtr)16;
		public static readonly IntPtr RT_RCDATA = (IntPtr)10;

		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		public struct GROUPICON
		{
			public short Reserved1;
			public short ResourceType;
			public short ImageCount;
			public byte Width;
			public byte Height;
			public byte Colors;
			public byte Reserved2;
			public short Planes;
			public short BitsPerPixel;
			public int ImageSize;
			public short ResourceID;
		};
		public static readonly int GROUPICONSIZE = Marshal.SizeOf<GROUPICON>();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, ref GROUPICON lpData, int cbData);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, int cbData);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LockResource(IntPtr hResData);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

		[DllImport("version.dll", SetLastError = true)]
		public static extern bool GetFileVersionInfo(string lptstrFilename, int dwHandleIgnored, int dwLen, byte[] lpData);
		[DllImport("version.dll", SetLastError = true)]
		public static extern int GetFileVersionInfoSize(string lptstrFilename, IntPtr lpdwHandle);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern void SetDllDirectory(string lpPathName);

	}
}
