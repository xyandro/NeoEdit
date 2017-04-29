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
	partial class EditColorDialog
	{
		public class Result
		{
			public string Color { get; set; }
		}

		[DepProp]
		public byte Alpha { get { return UIHelper<EditColorDialog>.GetPropValue<byte>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Red { get { return UIHelper<EditColorDialog>.GetPropValue<byte>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Green { get { return UIHelper<EditColorDialog>.GetPropValue<byte>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public byte Blue { get { return UIHelper<EditColorDialog>.GetPropValue<byte>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Percent { get { return UIHelper<EditColorDialog>.GetPropValue<string>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Picking { get { return UIHelper<EditColorDialog>.GetPropValue<bool>(this); } set { UIHelper<EditColorDialog>.SetPropValue(this, value); } }

		static EditColorDialog() { UIHelper<EditColorDialog>.Register(); }

		EditColorDialog(string color)
		{
			InitializeComponent();

			byte alpha, red, green, blue;
			ColorConverter.GetARGB(color, out alpha, out red, out green, out blue);
			Alpha = alpha;
			Red = red;
			Green = green;
			Blue = blue;
		}

		byte Adjust(byte color, double percent) => (byte)Math.Max(0, Math.Min((int)(color * percent / 100 + 0.5), 255));

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(Percent))
			{
				var percent = double.Parse(Percent);
				Red = Adjust(Red, percent);
				Green = Adjust(Green, percent);
				Blue = Adjust(Blue, percent);
				Percent = null;
				return;
			}

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
			var dialog = new EditColorDialog(color) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}

	class ColorConverter : IMultiValueConverter
	{
		string lastValue;

		public static bool GetARGB(string color, out byte alpha, out byte red, out byte green, out byte blue)
		{
			alpha = red = green = blue = 0;
			if (string.IsNullOrEmpty(color))
				return false;

			if (color.Length <= 3)
			{
				color = color.PadLeft(3, '0');
				alpha = 255;
				red = byte.Parse(color.Substring(0, 1), NumberStyles.HexNumber);
				green = byte.Parse(color.Substring(1, 1), NumberStyles.HexNumber);
				blue = byte.Parse(color.Substring(2, 1), NumberStyles.HexNumber);
				red = (byte)(red * 16 + red);
				green = (byte)(green * 16 + green);
				blue = (byte)(blue * 16 + blue);
			}
			else if (color.Length <= 6)
			{
				color = color.PadLeft(6, '0');
				alpha = 255;
				red = byte.Parse(color.Substring(0, 2), NumberStyles.HexNumber);
				green = byte.Parse(color.Substring(2, 2), NumberStyles.HexNumber);
				blue = byte.Parse(color.Substring(4, 2), NumberStyles.HexNumber);
			}
			else if (color.Length <= 8)
			{
				color = color.PadLeft(8, '0');
				alpha = byte.Parse(color.Substring(0, 2), NumberStyles.HexNumber);
				red = byte.Parse(color.Substring(2, 2), NumberStyles.HexNumber);
				green = byte.Parse(color.Substring(4, 2), NumberStyles.HexNumber);
				blue = byte.Parse(color.Substring(6, 2), NumberStyles.HexNumber);
			}
			else
				return false;
			return true;
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			byte alpha2, red2, green2, blue2;
			var alpha = (byte)values[0];
			var red = (byte)values[1];
			var green = (byte)values[2];
			var blue = (byte)values[3];
			if (GetARGB(lastValue, out alpha2, out red2, out green2, out blue2))
			{
				if ((alpha == alpha2) && (red == red2) && (green == green2) && (blue == blue2))
					return lastValue;
			}
			return alpha == 255 ? $"{red:x2}{green:x2}{blue:x2}" : $"{alpha:x2}{red:x2}{green:x2}{blue:x2}";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			byte alpha, red, green, blue;
			lastValue = value as string;
			if (GetARGB(lastValue, out alpha, out red, out green, out blue))
				return new object[] { alpha, red, green, blue };
			return new object[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
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
