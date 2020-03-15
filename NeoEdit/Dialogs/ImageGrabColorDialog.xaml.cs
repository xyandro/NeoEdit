using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class ImageGrabColorDialog
	{
		public class Result
		{
			public List<string> Colors { get; set; }
		}

		[DepProp]
		public byte Alpha { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<byte>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Red { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<byte>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Green { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<byte>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Blue { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<byte>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tracking { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> Colors { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }

		static ImageGrabColorDialog()
		{
			UIHelper<ImageGrabColorDialog>.Register();
			UIHelper<ImageGrabColorDialog>.AddCallback(x => x.Tracking, (obj, o, n) => obj.SetColorFromMousePosition());
		}

		IntPtr hook;
		readonly Win32.HookProc hookProc; // Must remain live while it might be called
		ImageGrabColorDialog(string color)
		{
			hookProc = MouseHook;
			InitializeComponent();

			try
			{
				Colorer.StringToARGB(color, out var alpha, out var red, out var green, out var blue);
				Alpha = alpha;
				Red = red;
				Green = green;
				Blue = blue;
			}
			catch { Alpha = Red = Green = Blue = 255; }
			Colors = new ObservableCollection<string>();
			Loaded += (s, e) =>
			{
				using (var process = Process.GetCurrentProcess())
				using (var module = process.MainModule)
					hook = Win32.SetWindowsHookEx(Win32.WH_MOUSE_LL, hookProc, Win32.GetModuleHandle(module.ModuleName), 0);
			};
		}

		protected override void OnClosed(EventArgs e)
		{
			Win32.UnhookWindowsHookEx(hook);
			while (hidden.Count > 0)
				Win32.ShowWindow(hidden.Pop(), Win32.SW_SHOWNA);
			base.OnClosed(e);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!Colors.Any())
				AddClick(sender, e);
			result = new Result { Colors = Colors.ToList() };
			DialogResult = true;
		}

		void AddClick(object sender, RoutedEventArgs e)
		{
			Colors.Add(color.Text);
			e.Handled = true;
		}

		readonly Stack<IntPtr> hidden = new Stack<IntPtr>();
		void HideClick(object sender, RoutedEventArgs e)
		{
			Win32.GetCursorPos(out var point);
			var hwnd = Win32.WindowFromPoint(point);

			if (hwnd == new WindowInteropHelper(this).Handle)
			{
				Rect ownerRect;
				if (Owner.WindowState == WindowState.Maximized)
				{
					var useRect = System.Windows.Forms.Screen.GetWorkingArea(new System.Drawing.Point((int)Owner.Left, (int)Owner.Top));
					ownerRect = new Rect(useRect.X, useRect.Y, useRect.Width, useRect.Height);
				}
				else
					ownerRect = new Rect(Owner.Left, Owner.Top, Owner.ActualWidth, Owner.ActualHeight);

				var points = new List<Point>
				{
					new Point(ownerRect.Left + 10, ownerRect.Top + 10), // Top left
					new Point(ownerRect.Left + ownerRect.Width - ActualWidth - 10, ownerRect.Top + 10), // Top right
					new Point(ownerRect.Left + 10, ownerRect.Top + ownerRect.Height - ActualHeight - 10), // Bottom left
					new Point(ownerRect.Left - 10 + ownerRect.Width - ActualWidth, ownerRect.Top + ownerRect.Height - ActualHeight - 10), // Bottom right
				};

				var current = new Point(point.X, point.Y);

				var furthestPoint = points.OrderByDescending(testPoint => (testPoint - current).LengthSquared).First();

				Left = furthestPoint.X;
				Top = furthestPoint.Y;

				return;
			}

			hwnd = Win32.GetAncestor(hwnd, Win32.GA_ROOT);
			if (hwnd == Win32.GetDesktopWindow())
				return;

			Win32.ShowWindow(hwnd, Win32.SW_HIDE);
			hidden.Push(hwnd);
			e.Handled = true;
		}

		IntPtr MouseHook(int code, IntPtr wParam, IntPtr lParam)
		{
			if ((int)wParam == Win32.WM_MOUSEMOVE)
				SetColorFromMousePosition();

			return Win32.CallNextHookEx(hook, code, wParam, lParam);
		}

		void SetColorFromMousePosition(MouseEventArgs e = null)
		{
			if (!Tracking)
				return;

			Win32.GetCursorPos(out var point);
			using (var screenPixel = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			using (var dest = System.Drawing.Graphics.FromImage(screenPixel))
			{
				dest.CopyFromScreen(point.X, point.Y, 0, 0, new System.Drawing.Size(1, 1));
				var color = screenPixel.GetPixel(0, 0);
				Alpha = color.A;
				Red = color.R;
				Green = color.G;
				Blue = color.B;
			}
		}

		static public Result Run(Window parent, string color)
		{
			var dialog = new ImageGrabColorDialog(color) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}

		class Win32
		{
			public const int GA_ROOT = 2;
			public const int SW_HIDE = 0;
			public const int SW_SHOWNA = 8;
			public const int WH_MOUSE_LL = 14;
			public const int WM_MOUSEMOVE = 512;

			public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

			[StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

			[DllImport("user32.dll", SetLastError = false)] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
			[DllImport("user32.dll", SetLastError = false)] public static extern IntPtr GetAncestor(IntPtr hwnd, int gaFlags);
			[DllImport("user32.dll", SetLastError = false)] public static extern bool GetCursorPos(out POINT lpPoint);
			[DllImport("user32.dll", SetLastError = false)] public static extern IntPtr GetDesktopWindow();
			[DllImport("kernel32.dll", SetLastError = false)] public static extern IntPtr GetModuleHandle(string lpModuleName);
			[DllImport("user32.dll", SetLastError = false)] public static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);
			[DllImport("user32.dll", SetLastError = false)] public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
			[DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
			[DllImport("user32.dll", SetLastError = false)] public static extern IntPtr WindowFromPoint(POINT Point);
		}
	}

	class ColorConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Colorer.ARGBToString((byte)values[0], (byte)values[1], (byte)values[2], (byte)values[3]);

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			Colorer.StringToARGB(value as string, out var alpha, out var red, out var green, out var blue);
			return new object[] { alpha, red, green, blue };
		}
	}

	class SampleColorConverter : MarkupExtension, IMultiValueConverter
	{
		static SampleColorConverter converter = new SampleColorConverter();
		public override object ProvideValue(IServiceProvider serviceProvider) => converter;
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => new SolidColorBrush(new Color { A = (byte)values[0], R = (byte)values[1], G = (byte)values[2], B = (byte)values[3] });
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
