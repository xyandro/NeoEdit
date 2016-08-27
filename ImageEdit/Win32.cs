using System.Runtime.InteropServices;

namespace NeoEdit.ImageEdit
{
	static class Win32
	{
		[DllImport("User32.Dll")]
		public static extern long SetCursorPos(int x, int y);
	}
}
