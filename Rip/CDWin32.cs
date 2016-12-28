using System;
using System.Runtime.InteropServices;

namespace NeoEdit.Rip
{
	static class CDWin32
	{
		public const int SectorBytes = 2352;

		[Flags]
		public enum CreationDisposition : uint
		{
			OPEN_EXISTING = 3,
		}

		[Flags]
		public enum DesiredAccess : uint
		{
			GENERIC_READ = 0x80000000,
		}

		[Flags]
		public enum IoControlCode : uint
		{
			IOCTL_STORAGE_CHECK_VERIFY = 0x002D4800,
			IOCTL_CDROM_READ_TOC = 0x00024000,
			IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804,
			IOCTL_CDROM_RAW_READ = 0x0002403E,
		}

		[Flags]
		public enum ShareMode : uint
		{
			FILE_SHARE_READ = 0x00000001,
		}

		public enum TRACK_MODE_TYPE { YellowMode2, XAForm2, CDDA }

		[StructLayout(LayoutKind.Sequential)]
		public struct TRACK_DATA
		{
			byte Reserved;
			public byte BitMapped;
			byte TrackNumber;
			byte Reserved1;
			byte Address_0;
			byte Address_1;
			byte Address_2;
			byte Address_3;

			public int Addr => Address_1 * 4500 + Address_2 * 75 + Address_3 - 150;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class CDROM_TOC
		{
			public ushort Length;
			public byte FirstTrack = 0;
			public byte LastTrack = 0;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
			public TRACK_DATA[] TrackData;

			public CDROM_TOC()
			{
				Length = (ushort)Marshal.SizeOf(this);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class PREVENT_MEDIA_REMOVAL
		{
			public byte PreventMediaRemoval = 0;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class RAW_READ_INFO
		{
			public long DiskOffset = 0;
			public int SectorCount = 0;
			public TRACK_MODE_TYPE TrackMode = TRACK_MODE_TYPE.CDDA;
		}

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool CloseHandle(IntPtr hObject);

		[DllImport("Kernel32.dll", SetLastError = true)]
		public extern static IntPtr CreateFile(string fileName, DesiredAccess desiredAccess, ShareMode shareMode, IntPtr lpSecurityAttributes, CreationDisposition creationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(IntPtr hDevice, IoControlCode ioControlCode, IntPtr lpInBuffer, int InBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(IntPtr hDevice, IoControlCode ioControlCode, IntPtr InBuffer, int InBufferSize, [Out] CDROM_TOC OutTOC, int OutBufferSize, ref int BytesReturned, IntPtr Overlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(IntPtr hDevice, IoControlCode ioControlCode, [In] PREVENT_MEDIA_REMOVAL InMediaRemoval, int InBufferSize, IntPtr OutBuffer, int OutBufferSize, ref int BytesReturned, IntPtr Overlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(IntPtr hDevice, IoControlCode ioControlCode, [In] RAW_READ_INFO rri, int InBufferSize, [In, Out] byte[] OutBuffer, int OutBufferSize, ref int BytesReturned, IntPtr Overlapped);
	}
}
