using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ImageGrabImageDialog
	{
		[DepProp]
		public string GrabX { get { return UIHelper<ImageGrabImageDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGrabImageDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string GrabY { get { return UIHelper<ImageGrabImageDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGrabImageDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string GrabWidth { get { return UIHelper<ImageGrabImageDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGrabImageDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string GrabHeight { get { return UIHelper<ImageGrabImageDialog>.GetPropValue<string>(this); } set { UIHelper<ImageGrabImageDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static ImageGrabImageDialog() { UIHelper<ImageGrabImageDialog>.Register(); }

		IntPtr hook;
		readonly Win32.HookProc hookProc;
		ImageGrabImageDialog(NEVariables variables)
		{
			Variables = variables;
			hookProc = MouseHook;

			InitializeComponent();

			Loaded += (s, e) =>
			{
				using (var process = Process.GetCurrentProcess())
				using (var module = process.MainModule)
					hook = Win32.SetWindowsHookEx(Win32.HookType.WH_MOUSE_LL, hookProc, Win32.GetModuleHandle(module.ModuleName), 0);
			};
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Win32.UnhookWindowsHookEx(hook);
		}

		int startX, startY;
		bool tracking = false;
		IntPtr MouseHook(int code, IntPtr wParam, IntPtr lParam)
		{
			var result = default(IntPtr?);
			var message = (Win32.Message)wParam;
			if (Enum.IsDefined(typeof(Win32.Message), message))
			{
				var hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MSLLHOOKSTRUCT));
				var curX = hookStruct.pt.X;
				var curY = hookStruct.pt.Y;

				if (message == Win32.Message.WM_LBUTTONDOWN)
				{
					tracking = true;
					startX = curX;
					startY = curY;
					result = (IntPtr)1;
				}

				if (tracking)
				{
					GrabX = Math.Min(startX, curX).ToString();
					GrabY = Math.Min(startY, curY).ToString();
					GrabWidth = Math.Abs(startX - curX).ToString();
					GrabHeight = Math.Abs(startY - curY).ToString();
				}

				if (message == Win32.Message.WM_LBUTTONUP)
				{
					tracking = false;
					result = (IntPtr)1;
				}
			}

			result = result ?? Win32.CallNextHookEx(hook, code, wParam, lParam);
			return result.Value;
		}

		Configuration_Image_GrabImage result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_GrabImage
			{
				GrabX = GrabX,
				GrabY = GrabY,
				GrabWidth = GrabWidth,
				GrabHeight = GrabHeight,
			};
			DialogResult = true;
		}

		public static Configuration_Image_GrabImage Run(Window parent, NEVariables variables)
		{
			var dialog = new ImageGrabImageDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}

		class Win32
		{
			public enum Message
			{
				WM_MOUSEMOVE = 512,
				WM_LBUTTONDOWN = 513,
				WM_LBUTTONUP = 514,
			}

			public enum HookType : int
			{
				WH_MOUSE_LL = 14
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct POINT
			{
				public int X, Y;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct MSLLHOOKSTRUCT
			{
				public POINT pt;
				public uint mouseData;
				public uint flags;
				public uint time;
				public IntPtr dwExtraInfo;
			}

			public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll", SetLastError = true)] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
			[DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr GetModuleHandle(string lpModuleName);
			[DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);
			[DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
		}
	}
}
