using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NeoEdit.GUI.Common
{
	public class Transparency
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct TransparentMargins { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;		}

		[DllImport("DwmApi.dll")]
		public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, TransparentMargins pMarInset);

		public static void MakeTransparent(Window window)
		{
			window.Loaded += (s, e) =>
			{
				window.Background = Brushes.Transparent;
				var hwndSource = HwndSource.FromDependencyObject(window) as HwndSource;
				if (hwndSource == null)
					return;

				if (hwndSource.CompositionTarget != null)
					hwndSource.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(170, 0, 0, 0);
				DwmExtendFrameIntoClientArea(hwndSource.Handle, new TransparentMargins { cxLeftWidth = -1 });
			};
		}
	}
}
