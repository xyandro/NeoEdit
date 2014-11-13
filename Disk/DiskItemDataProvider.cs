using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace NeoEdit.Disk
{
	static class DiskItemDataProvider
	{
		const uint SHGFI_ICON = 0x000000100;
		const uint SHGFI_TYPENAME = 0x000000400;
		const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
		const uint SHGFI_LARGEICON = 0x000000000;

		const uint FILE_ATTRIBUTE_NORMAL = 0x80;
		const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

		[StructLayout(LayoutKind.Sequential)]
		struct SHFILEINFO
		{
			public const int MAX_PATH = 260;
			public const int NAMESIZE = 80;
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = NAMESIZE)]
			public string szTypeName;
		};

		[DllImport("Shell32.dll")]
		static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool DestroyIcon(IntPtr hIcon);

		public static void GetExtraData(string path, bool folder, out BitmapSource icon, out string typeName)
		{
			icon = null;
			typeName = null;

			var shfi = new SHFILEINFO();
			if (SHGetFileInfo(path, folder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref shfi, Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES | SHGFI_LARGEICON) == IntPtr.Zero)
				return;

			icon = Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			typeName = shfi.szTypeName;
			DestroyIcon(shfi.hIcon);
		}
	}
}
