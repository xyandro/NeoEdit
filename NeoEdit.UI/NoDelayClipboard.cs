using System;
using System.Runtime.InteropServices;

namespace NeoEdit.UI
{
	static class NoDelayClipboard
	{
		public static void SetText(string text)
		{
			OpenClipboard(IntPtr.Zero);
			var hg = Marshal.StringToHGlobalUni(text);
			SetClipboardData(CF_UNICODETEXT, hg);
			CloseClipboard();
			Marshal.FreeHGlobal(hg);
		}

		const int GMEM_MOVEABLE = 0x0002;
		const int CF_UNICODETEXT = 13;
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool OpenClipboard(IntPtr hWndNewOwner);
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool CloseClipboard();
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetClipboardData(uint uFormat, IntPtr data);
	}
}
