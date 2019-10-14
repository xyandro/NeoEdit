using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.Program.Converters;
using Newtonsoft.Json;

namespace NeoEdit.Program.Controls
{
	public class NEWindow : Window
	{
		const double ResizeBorder = 5;
		const double DragDetect = 10;

		[DepProp]
		public bool IsMainWindow { get { return UIHelper<NEWindow>.GetPropValue<bool>(this); } set { UIHelper<NEWindow>.SetPropValue(this, value); } }

		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(32, 32, 32));
		static readonly Brush OutlineBrush = Brushes.White;
		static readonly BoolToVisibleConverter boolToVisibleConverter = new BoolToVisibleConverter();

		Borders saveBorder;
		Point savePoint;
		Rect saveWindowPosition;

		static NEWindow()
		{
			UIHelper<NEWindow>.Register();
			BackgroundBrush.Freeze();
			OutlineBrush.Freeze();
		}

		public NEWindow()
		{
			WindowStyle = WindowStyle.None;
			Visibility = Visibility.Visible;
			ResizeMode = ResizeMode.CanResizeWithGrip;
			AllowsTransparency = true;
			Template = GetTemplate();
		}

		public new bool ShowDialog()
		{
			var result = base.ShowDialog() == true;
			Owner?.Focus();
			return result;
		}

		ControlTemplate GetTemplate()
		{
			var template = new ControlTemplate(typeof(NEWindow));

			var outerGrid = new FrameworkElementFactory(typeof(Grid));

			var rect = new FrameworkElementFactory(typeof(Rectangle));
			rect.Name = "PART_rect";
			rect.SetValue(Rectangle.FillProperty, Brushes.Black);
			rect.SetValue(Rectangle.VisibilityProperty, Visibility.Hidden);
			outerGrid.AppendChild(rect);

			var rectTrigger = new Trigger { Property = WindowStateProperty, Value = WindowState.Maximized };
			rectTrigger.Setters.Add(new Setter { TargetName = rect.Name, Property = VisibilityProperty, Value = Visibility.Visible });
			template.Triggers.Add(rectTrigger);

			var border = new FrameworkElementFactory(typeof(Border));
			border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
			border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			border.SetValue(Border.BackgroundProperty, BackgroundBrush);
			border.SetValue(Border.BorderBrushProperty, OutlineBrush);
			border.AddHandler(Border.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonDown));
			border.AddHandler(Border.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonUp));
			border.AddHandler(Border.MouseMoveEvent, new MouseEventHandler(OnBorderMouseMove));

			var grid = new FrameworkElementFactory(typeof(Grid));

			var gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
			grid.AppendChild(gridItem);

			gridItem = new FrameworkElementFactory(typeof(RowDefinition));
			gridItem.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(RowDefinition));
			gridItem.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
			grid.AppendChild(gridItem);

			var decoder = BitmapDecoder.Create(new Uri($"pack://application:,,,/NeoEdit;component/NeoEdit.ico"), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			var iconImage = decoder.Frames.Where(f => f.Width == 16).OrderByDescending(f => f.Width).First();
			var icon = new FrameworkElementFactory(typeof(Image));
			icon.SetValue(Image.StretchProperty, Stretch.None);
			icon.SetValue(Image.SourceProperty, iconImage);
			icon.SetValue(Image.MarginProperty, new Thickness(5, 0, 0, 0));
			icon.SetBinding(Image.VisibilityProperty, new Binding(nameof(IsMainWindow)) { Source = this, Converter = boolToVisibleConverter });
			icon.SetValue(Grid.RowProperty, 0);
			icon.SetValue(Grid.ColumnProperty, 0);
			grid.AppendChild(icon);

			var textBlock = new FrameworkElementFactory(typeof(TextBlock));
			textBlock.SetValue(TextBlock.FontSizeProperty, 14d);
			textBlock.SetValue(TextBlock.ForegroundProperty, Brushes.White);
			textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
			textBlock.SetValue(Image.MarginProperty, new Thickness(10, 0, 10, 0));
			textBlock.AddHandler(TextBlock.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnTitleMouseLeftButtonDown));
			textBlock.AddHandler(TextBlock.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnTitleMouseLeftButtonUp));
			textBlock.AddHandler(TextBlock.MouseMoveEvent, new MouseEventHandler(OnTitleMouseMove));
			textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Title)) { Source = this });
			textBlock.SetValue(Grid.RowProperty, 0);
			textBlock.SetValue(Grid.ColumnProperty, 1);
			grid.AppendChild(textBlock);

			var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
			stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			stackPanel.SetValue(Grid.RowProperty, 0);
			stackPanel.SetValue(Grid.ColumnProperty, 2);

			var minimizeButton = new FrameworkElementFactory(typeof(Button));
			minimizeButton.SetValue(Button.ContentProperty, "🗕");
			minimizeButton.SetValue(Button.FontSizeProperty, 14d);
			minimizeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			minimizeButton.SetValue(Button.ForegroundProperty, Brushes.White);
			minimizeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			minimizeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			minimizeButton.SetBinding(Button.VisibilityProperty, new Binding(nameof(IsMainWindow)) { Source = this, Converter = boolToVisibleConverter });
			minimizeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnMinimizeClick));
			stackPanel.AppendChild(minimizeButton);

			var maximizeButton = new FrameworkElementFactory(typeof(Button));
			maximizeButton.Name = "maximizeButton";
			maximizeButton.SetValue(Button.FontSizeProperty, 14d);
			maximizeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			maximizeButton.SetValue(Button.ForegroundProperty, Brushes.White);
			maximizeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			maximizeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			maximizeButton.SetValue(Button.ContentProperty, "🗖");
			maximizeButton.SetBinding(Button.VisibilityProperty, new Binding(nameof(IsMainWindow)) { Source = this, Converter = boolToVisibleConverter });
			maximizeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnRestoreClick));
			stackPanel.AppendChild(maximizeButton);

			var maximizeTrigger = new Trigger { Property = WindowStateProperty, Value = WindowState.Maximized };
			maximizeTrigger.Setters.Add(new Setter { TargetName = maximizeButton.Name, Property = Button.ContentProperty, Value = "🗗" });
			template.Triggers.Add(maximizeTrigger);

			var closeButton = new FrameworkElementFactory(typeof(Button));
			closeButton.SetValue(Button.ContentProperty, "🗙");
			closeButton.SetValue(Button.FontSizeProperty, 14d);
			closeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			closeButton.SetValue(Button.ForegroundProperty, Brushes.White);
			closeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			closeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			closeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnCloseClick));

			stackPanel.AppendChild(closeButton);
			grid.AppendChild(stackPanel);

			var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
			contentPresenter.SetValue(ContentPresenter.FocusVisualStyleProperty, null);
			contentPresenter.SetValue(Grid.RowProperty, 1);
			contentPresenter.SetValue(Grid.ColumnProperty, 0);
			contentPresenter.SetValue(Grid.ColumnSpanProperty, 3);
			grid.AppendChild(contentPresenter);

			border.AppendChild(grid);

			outerGrid.AppendChild(border);

			template.VisualTree = outerGrid;

			return template;
		}

		[Flags]
		enum Borders
		{
			None = 0,
			Left = 1,
			Right = 2,
			Top = 4,
			Bottom = 8,
			TopLeft = Top | Left,
			TopRight = Top | Right,
			BottomLeft = Bottom | Left,
			BottomRight = Bottom | Right,
		}

		Borders GetMouseBorder(Point pos)
		{
			if (WindowState == WindowState.Maximized)
				return Borders.None;

			var moveBorder = Borders.None;
			if (pos.X <= ResizeBorder)
				moveBorder |= Borders.Left;
			else if (pos.X >= ActualWidth - ResizeBorder)
				moveBorder |= Borders.Right;
			if (pos.Y <= ResizeBorder)
				moveBorder |= Borders.Top;
			else if (pos.Y >= ActualHeight - ResizeBorder)
				moveBorder |= Borders.Bottom;
			return moveBorder;
		}

		void OnBorderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var border = sender as Border;
			var pos = e.GetPosition(border);
			saveBorder = GetMouseBorder(pos);
			if (saveBorder == Borders.None)
				return;

			savePoint = border.PointToScreen(pos);
			saveWindowPosition = new Rect(Left, Top, ActualWidth, ActualHeight);
			border.CaptureMouse();
			e.Handled = true;
		}

		void OnBorderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var border = sender as Border;
			if (border.IsMouseCaptured)
			{
				border.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		void OnBorderMouseMove(object sender, MouseEventArgs e)
		{
			var border = sender as Border;
			var pos = e.GetPosition(border);

			if (border.IsMouseCaptured)
			{
				e.Handled = true;
				var diff = border.PointToScreen(pos) - savePoint;
				if (saveBorder.HasFlag(Borders.Left))
				{
					Left = saveWindowPosition.Left + diff.X;
					Width = saveWindowPosition.Width - diff.X;
				}
				if (saveBorder.HasFlag(Borders.Right))
					Width = saveWindowPosition.Width + diff.X;
				if (saveBorder.HasFlag(Borders.Top))
				{
					Top = saveWindowPosition.Top + diff.Y;
					Height = saveWindowPosition.Height - diff.Y;
				}
				if (saveBorder.HasFlag(Borders.Bottom))
					Height = saveWindowPosition.Height + diff.Y;
			}
			else
			{
				switch (GetMouseBorder(pos))
				{
					case Borders.Left: case Borders.Right: border.Cursor = Cursors.SizeWE; break;
					case Borders.Top: case Borders.Bottom: border.Cursor = Cursors.SizeNS; break;
					case Borders.TopLeft: case Borders.BottomRight: border.Cursor = Cursors.SizeNWSE; break;
					case Borders.TopRight: case Borders.BottomLeft: border.Cursor = Cursors.SizeNESW; break;
					default: border.Cursor = null; break;
				}
			}
		}

		void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				OnRestoreClick(null, null);
			else if (WindowState == WindowState.Maximized)
			{
				savePoint = e.GetPosition(this);
				(sender as TextBlock).CaptureMouse();
			}
			else
				DragMove();
			e.Handled = true;
		}

		void OnTitleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var title = sender as TextBlock;
			if (title.IsMouseCaptured)
			{
				title.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		void OnTitleMouseMove(object sender, MouseEventArgs e)
		{
			var title = sender as TextBlock;
			if (!title.IsMouseCaptured)
				return;

			var newPoint = e.GetPosition(this);
			if ((newPoint - savePoint).Length >= DragDetect)
			{
				title.ReleaseMouseCapture();

				var startPos = PointToScreen(newPoint);
				WindowState = WindowState.Normal;
				var dist = PointToScreen(savePoint) - startPos;
				Left = startPos.X - ActualWidth / 2;
				Top -= dist.Y;
				DragMove();
			}
			e.Handled = true;
		}

		void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

		void OnRestoreClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

		void OnCloseClick(object sender, RoutedEventArgs e) => Close();

		protected override void OnSourceInitialized(EventArgs e)
		{
			HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(new HwndSourceHook(Win32.WindowProc));
			base.OnSourceInitialized(e);
		}

		public string GetPosition() => Win32.GetPosition(new WindowInteropHelper(this).Handle);

		public void SetPosition(string position) => Win32.SetPosition(new WindowInteropHelper(this).Handle, position);

		static class Win32
		{
			internal static string GetPosition(IntPtr handle)
			{
				var placement = new WINDOWPLACEMENT();
				GetWindowPlacement(handle, out placement);
				return JsonConvert.SerializeObject(placement);
			}

			internal static void SetPosition(IntPtr handle, string position)
			{
				if (string.IsNullOrEmpty(position))
					return;

				WINDOWPLACEMENT placement;
				placement = JsonConvert.DeserializeObject<WINDOWPLACEMENT>(position);
				placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				placement.flags = 0;
				placement.showCmd = placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd;
				SetWindowPlacement(handle, ref placement);
			}

			internal static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				if (msg == Win32.WM_GETMINMAXINFO)
					Win32.GetMinMaxInfo(hwnd, lParam);

				return IntPtr.Zero;
			}

			static void GetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
			{
				var primaryMonitor = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);
				var monitorInfo = new MONITORINFO();
				if (!GetMonitorInfo(primaryMonitor, monitorInfo))
					return;

				var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
				mmi.ptMaxPosition.X = monitorInfo.rcWork.Left;
				mmi.ptMaxPosition.Y = monitorInfo.rcWork.Top;
				mmi.ptMaxSize.X = monitorInfo.rcWork.Right - monitorInfo.rcWork.Left;
				mmi.ptMaxSize.Y = monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top;
				Marshal.StructureToPtr(mmi, lParam, true);
			}

			const int WM_GETMINMAXINFO = 0x0024;
			const int SW_SHOWNORMAL = 1;
			const int SW_SHOWMINIMIZED = 2;
			const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
			const int MONITOR_DEFAULTTONEAREST = 0x00000002;

			[DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);
			[DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
			[DllImport("user32.dll")] static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);
			[DllImport("user32.dll")] static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			struct POINT
			{
				public int X, Y;
				public static POINT FromPoint(Point point) => new POINT { X = (int)(point.X + 0.5), Y = (int)(point.Y + 0.5) };
			}

			[StructLayout(LayoutKind.Sequential)]
			struct MINMAXINFO
			{
				public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
			};

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
			class MONITORINFO
			{
				public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
				public RECT rcMonitor = new RECT();
				public RECT rcWork = new RECT();
				public int dwFlags = 0;
			}

			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			struct RECT
			{
				public int Left, Top, Right, Bottom;
			}

			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			struct WINDOWPLACEMENT
			{
				public int length, flags, showCmd;
				public POINT minPosition, maxPosition;
				public RECT normalPosition;
			}
		}
	}
}
