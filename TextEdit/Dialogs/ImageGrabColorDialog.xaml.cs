using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ImageGrabColorDialog
	{
		public class Result
		{
			public string Color { get; set; }
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
		public bool Picking { get { return UIHelper<ImageGrabColorDialog>.GetPropValue<bool>(this); } set { UIHelper<ImageGrabColorDialog>.SetPropValue(this, value); } }

		static ImageGrabColorDialog() { UIHelper<ImageGrabColorDialog>.Register(); }

		ImageGrabColorDialog(string color)
		{
			InitializeComponent();

			byte alpha = 255, red = 255, green = 255, blue = 255;
			try { ColorConverter.GetARGB(color, out alpha, out red, out green, out blue); } catch { }
			Alpha = alpha;
			Red = red;
			Green = green;
			Blue = blue;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			color.AddCurrentSuggestion();
			result = new Result { Color = color.Text };
			DialogResult = true;
		}

		Point pickStart;
		void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			CaptureMouse();
			Picking = true;
			pickStart = new Point(Left, Top);
			e.Handled = true;
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!IsMouseCaptured)
				return;

			using (var screenPixel = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			using (var dest = System.Drawing.Graphics.FromImage(screenPixel))
			{
				var pos = PointToScreen(e.GetPosition(this));
				dest.CopyFromScreen((int)pos.X, (int)pos.Y, 0, 0, new System.Drawing.Size(1, 1));
				var color = screenPixel.GetPixel(0, 0);
				Alpha = color.A;
				Red = color.R;
				Green = color.G;
				Blue = color.B;
			}
			e.Handled = true;
		}

		List<IntPtr> hidden = new List<IntPtr>();

		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!IsMouseCaptured)
				return;

			ReleaseMouseCapture();
			Picking = false;
			e.Handled = true;
			hidden.ForEach(hwnd => ShowWindow(hwnd, SW_SHOWNA));
			Left = pickStart.X;
			Top = pickStart.Y;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if ((!IsMouseCaptured) || (e.Key != Key.H))
				return;

			e.Handled = true;

			var pos = PointToScreen(Mouse.GetPosition(this));
			var point = new POINT { X = (int)pos.X, Y = (int)pos.Y };
			var hwnd = WindowFromPoint(point);

			if (hwnd == new WindowInteropHelper(this).Handle)
			{
				var points = new List<Point>
				{
					new Point(Owner.Left + 10, Owner.Top + 10), // Top left
					new Point(Owner.Left + Owner.ActualWidth - ActualWidth - 10, Owner.Top + 10), // Top right
					new Point(Owner.Left + 10, Owner.Top + Owner.ActualHeight - ActualHeight - 10), // Bottom left
					new Point(Owner.Left - 10 + Owner.ActualWidth - ActualWidth, Owner.Top + Owner.ActualHeight - ActualHeight - 10), // Bottom right
				};

				var current = new Point(Left, Top);

				var furthestPoint = points.OrderByDescending(testPoint => (testPoint - current).LengthSquared).First();

				Left = furthestPoint.X;
				Top = furthestPoint.Y;

				return;
			}

			hwnd = GetAncestor(hwnd, GA_ROOT);
			if (hwnd == GetDesktopWindow())
				return;

			ShowWindow(hwnd, SW_HIDE);
			hidden.Add(hwnd);
		}

		const int SW_HIDE = 0;
		const int SW_SHOWNA = 8;
		const int GA_ROOT = 2;

		[StructLayout(LayoutKind.Sequential)]
		struct POINT { public int X, Y; }

		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr WindowFromPoint(POINT Point);

		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr GetAncestor(IntPtr hwnd, int gaFlags);

		[DllImport("user32.dll", SetLastError = false)]
		static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr GetDesktopWindow();

		static public Result Run(Window parent, string color)
		{
			var dialog = new ImageGrabColorDialog(color) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}

	class ColorConverter : IMultiValueConverter
	{
		static byte GetValue(string color)
		{
			if ((string.IsNullOrEmpty(color)) || (color.Length > 2))
				throw new Exception("Invalid color");
			var value = byte.Parse(color, NumberStyles.HexNumber);
			if (color.Length == 1)
				value = (byte)(value * 16 + value);
			return value;
		}

		public static void GetARGB(string color, out byte alpha, out byte red, out byte green, out byte blue)
		{
			if (string.IsNullOrEmpty(color))
				throw new Exception($"Invalid color: {color}");
			foreach (var c in color)
				if (((c < '0') || (c > '9')) && ((c < 'A') || (c > 'F')) && ((c < 'a') || (c > 'f')))
					throw new Exception($"Invalid color: {color}");

			switch (color.Length)
			{
				case 1:
					alpha = 255;
					red = green = blue = GetValue(color.Substring(0, 1));
					break;
				case 2:
					alpha = 255;
					red = green = blue = GetValue(color.Substring(0, 2));
					break;
				case 3:
					alpha = 255;
					red = GetValue(color.Substring(0, 1));
					green = GetValue(color.Substring(1, 1));
					blue = GetValue(color.Substring(2, 1));
					break;
				case 4:
					alpha = GetValue(color.Substring(0, 1));
					red = GetValue(color.Substring(1, 1));
					green = GetValue(color.Substring(2, 1));
					blue = GetValue(color.Substring(3, 1));
					break;
				case 6:
					alpha = 255;
					red = GetValue(color.Substring(0, 2));
					green = GetValue(color.Substring(2, 2));
					blue = GetValue(color.Substring(4, 2));
					break;
				case 8:
					alpha = GetValue(color.Substring(0, 2));
					red = GetValue(color.Substring(2, 2));
					green = GetValue(color.Substring(4, 2));
					blue = GetValue(color.Substring(6, 2));
					break;
				default:
					throw new Exception($"Invalid color: {color}");
			}
		}

		public static string FromARGB(byte alpha, byte red, byte green, byte blue) => $"{alpha:x2}{red:x2}{green:x2}{blue:x2}";

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => FromARGB((byte)values[0], (byte)values[1], (byte)values[2], (byte)values[3]);

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			GetARGB(value as string, out byte alpha, out byte red, out byte green, out byte blue);
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
