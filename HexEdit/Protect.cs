using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace NeoEdit.HexEdit
{
	class Protect : IDisposable
	{
		readonly SafeProcessHandle handle;
		readonly VirtualQueryInfo info;
		readonly int protect;

		public Protect(SafeProcessHandle handle, VirtualQueryInfo info, int protect)
		{
			this.handle = handle;
			this.info = info;
			this.protect = protect;

			if (protect == info.Protect)
				return;

			int oldProtect;
			if (!Win32.VirtualProtectEx(handle.DangerousGetHandle(), (IntPtr)info.StartAddress, (IntPtr)info.RegionSize, protect, out oldProtect))
				throw new Win32Exception();
		}

		public void Dispose()
		{
			if (protect == info.Protect)
				return;

			int oldProtect;
			if (!Win32.VirtualProtectEx(handle.DangerousGetHandle(), (IntPtr)info.StartAddress, (IntPtr)info.RegionSize, info.Protect, out oldProtect))
				return;
			if (!Win32.FlushInstructionCache(handle.DangerousGetHandle(), (IntPtr)info.StartAddress, (IntPtr)info.RegionSize))
				return;
		}
	}
}
