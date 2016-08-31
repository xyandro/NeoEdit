using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NeoEdit.HexEdit
{
	class Win32
	{
		[Flags]
		public enum ProcessAccessFlags : uint
		{
			All = 0x001F0FFF,
			Terminate = 0x00000001,
			CreateThread = 0x00000002,
			VirtualMemoryOperation = 0x00000008,
			VirtualMemoryRead = 0x00000010,
			VirtualMemoryWrite = 0x00000020,
			DuplicateHandle = 0x00000040,
			CreateProcess = 0x000000080,
			SetQuota = 0x00000100,
			SetInformation = 0x00000200,
			QueryInformation = 0x00000400,
			QueryLimitedInformation = 0x00001000,
			Synchronize = 0x00100000
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION
		{
			public IntPtr BaseAddress;
			public IntPtr AllocationBase;
			public uint AllocationProtect;
			public IntPtr RegionSize;
			public uint State;
			public int Protect;
			public uint Type;

			static public int Size => Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
		}

		public const int ERROR_INVALID_PARAMETER = 87;
		public const int MEM_COMMIT = 0x1000;
		public const int MEM_MAPPED = 0x40000;
		public const int PAGE_EXECUTE = 0x10;
		public const int PAGE_EXECUTE_READ = 0x20;
		public const int PAGE_EXECUTE_READWRITE = 0x40;
		public const int PAGE_EXECUTE_WRITECOPY = 0x80;
		public const int PAGE_GUARD = 0x100;
		public const int PAGE_NOACCESS = 0x01;
		public const int PAGE_READONLY = 0x02;
		public const int PAGE_READWRITE = 0x04;
		public const int PAGE_WRITECOPY = 0x08;

		public static int LastError => Marshal.GetLastWin32Error();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr dwSize);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern SafeProcessHandle OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, int flNewProtect, out int lpflOldProtect);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
	}
}
