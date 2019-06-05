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
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Transform;

namespace NeoEdit.MenuImage.Dialogs
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

			try
			{
				Colorer.StringToARGB(color, out var alpha, out var red, out var green, out var blue);
				Alpha = alpha;
				Red = red;
				Green = green;
				Blue = blue;
			}
			catch { Alpha = Red = Green = Blue = 255; }
		}

		protected override void OnClosed(EventArgs e)
		{
			hidden.ForEach(hwnd => ShowWindow(hwnd, SW_SHOWNA));
			base.OnClosed(e);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			color.AddCurrentSuggestion();
			result = new Result { Color = color.Text };
			DialogResult = true;
		}

		void SelectClick(object sender, RoutedEventArgs e)
		{
			GetCursorPos(out var point);
			SetColor(new Point(point.X, point.Y));
			e.Handled = true;
		}

		void HideClick(object sender, RoutedEventArgs e)
		{
			GetCursorPos(out var point);
			HideWindow(new Point(point.X, point.Y));
			e.Handled = true;
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

			SetColor(PointToScreen(e.GetPosition(this)));
			e.Handled = true;
		}

		void SetColor(Point point)
		{
			using (var screenPixel = new System.Drawing.Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			using (var dest = System.Drawing.Graphics.FromImage(screenPixel))
			{
				dest.CopyFromScreen((int)point.X, (int)point.Y, 0, 0, new System.Drawing.Size(1, 1));
				var color = screenPixel.GetPixel(0, 0);
				Alpha = color.A;
				Red = color.R;
				Green = color.G;
				Blue = color.B;
			}
		}

		readonly List<IntPtr> hidden = new List<IntPtr>();

		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!IsMouseCaptured)
				return;

			ReleaseMouseCapture();
			Picking = false;
			e.Handled = true;
			Left = pickStart.X;
			Top = pickStart.Y;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if ((IsMouseCaptured) && (e.Key == Key.H))
			{
				HideWindow(PointToScreen(Mouse.GetPosition(this)));
				e.Handled = true;
			}

			base.OnPreviewKeyDown(e);
		}

		void HideWindow(Point point)
		{
			var winPoint = new POINT { X = (int)point.X, Y = (int)point.Y };
			var hwnd = WindowFromPoint(winPoint);

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

		[DllImport("user32.dll", SetLastError = false)]
		static extern bool GetCursorPos(out POINT lpPoint);

		static public Result Run(Window parent, string color)
		{
			var dialog = new ImageGrabColorDialog(color) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}

	class ColorConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Colorer.ARGBToString((byte)values[0], (byte)values[1], (byte)values[2], (byte)values[3]);

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			Colorer.StringToARGB(value as string, out byte alpha, out byte red, out byte green, out byte blue);
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
