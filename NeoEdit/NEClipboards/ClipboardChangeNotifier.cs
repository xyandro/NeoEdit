using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeoEdit.Program.NEClipboards
{
	[DesignerCategory("Code")]
	class ClipboardChangeNotifier : Form
	{
		readonly Action callback;
		public ClipboardChangeNotifier(Action callback)
		{
			this.callback = callback;
			IntPtr HWND_MESSAGE = new IntPtr(-3);
			SetParent(Handle, HWND_MESSAGE);
			AddClipboardFormatListener(Handle);
		}

		protected override void WndProc(ref Message m)
		{
			const int WM_CLIPBOARDUPDATE = 0x031D;
			if (m.Msg == WM_CLIPBOARDUPDATE)
				callback();
			base.WndProc(ref m);
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AddClipboardFormatListener(IntPtr hwnd);
	}
}
