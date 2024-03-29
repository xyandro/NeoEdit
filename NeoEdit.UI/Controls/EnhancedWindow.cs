﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.Common;
using Newtonsoft.Json;

namespace NeoEdit.UI.Controls
{
	public class EnhancedWindow : Window
	{
		const double MinWindowSize = 50;
		const double ResizeBorder = 10;
		const double DragDetect = 10;

		[DepProp]
		public bool IsMainWindow { get { return UIHelper<EnhancedWindow>.GetPropValue<bool>(this); } set { UIHelper<EnhancedWindow>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsFullScreen { get { return UIHelper<EnhancedWindow>.GetPropValue<bool>(this); } set { UIHelper<EnhancedWindow>.SetPropValue(this, value); } }

		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(32, 32, 32));
		static readonly Brush OuterBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
		static readonly Brush ActiveBrush = Brushes.White;
		static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));

		Borders saveBorder;
		Point savePoint;
		Rect saveWindowPosition;
		Rect nonFullScreenRect = new Rect(0, 0, 800, 600);
		readonly Win32.HookProc hookProc; // Must remain live while it might be called
		IntPtr hook;
		bool winDown;

		static EnhancedWindow()
		{
			UIHelper<EnhancedWindow>.Register();
			BackgroundBrush.Freeze();
			OuterBrush.Freeze();
			ActiveBrush.Freeze();
			InactiveBrush.Freeze();
		}

		List<Rect> GetMonitors()
		{
			var monitors = new List<Rect>();
			bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam)
			{
				var monitorInfo = new Win32.MONITORINFO();
				Win32.GetMonitorInfo(monitor, monitorInfo);
				monitors.Add(new Rect(monitorInfo.rcWork.Left, monitorInfo.rcWork.Top, monitorInfo.rcWork.Right - monitorInfo.rcWork.Left, monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top));
				return true;
			}

			Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
			return monitors;
		}

		Rect GetMainMonitor()
		{
			var useRect = IsFullScreen ? nonFullScreenRect : new Rect(Left, Top, Width, Height);

			return GetMonitors().OrderByDescending(x =>
			{
				var intersect = Rect.Intersect(useRect, x);
				if (intersect == Rect.Empty)
					intersect = new Rect();
				return (int)((intersect.Width * intersect.Height) / (x.Width * x.Height) * 100 + 0.5);
			}).First();
		}

		List<(Size, List<Rect>)> GetFullScreenRects()
		{
			Rect GetRect(List<Rect> rects)
			{
				var left = rects.Min(x => x.Left);
				var top = rects.Min(x => x.Top);
				var right = rects.Max(x => x.Right);
				var bottom = rects.Max(x => x.Bottom);

				var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
				var p = transform.Transform(new Point(left, top));
				var v = transform.Transform(new Vector(right - left, bottom - top));

				return new Rect(p, v);
			}

			var mustInclude = GetMainMonitor();
			var fullScreenRects = new List<(Size, List<Rect>)>();
			var useMonitors = new List<List<Rect>> { new List<Rect>() };
			foreach (var monitor in GetMonitors())
			{
				var newInclude = new List<List<Rect>>();
				foreach (var value in useMonitors)
				{
					newInclude.Add(value.Concat(monitor).ToList());
					if (monitor != mustInclude)
						newInclude.Add(value.ToList());
				}
				useMonitors = newInclude;
			}
			fullScreenRects.AddRange(useMonitors.Where(list => list.Any()).Select(GetRect).Distinct().GroupBy(x => x.Size).Select(g => (g.Key, g.ToList())).OrderBy(tuple => tuple.Item1.Width * tuple.Item1.Height).ThenBy(tuple => tuple.Item1.Width));
			return fullScreenRects;
		}

		public EnhancedWindow()
		{
			hookProc = HookProc;
			WindowStyle = WindowStyle.None;
			Visibility = Visibility.Visible;
			ResizeMode = ResizeMode.CanResizeWithGrip;
			AllowsTransparency = true;
			Template = GetTemplate();
		}

		public new bool ShowDialog()
		{
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var result = base.ShowDialog() == true;
			Owner?.Focus();
			return result;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if ((Owner != null) && (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt)))
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.Up: SetWindowPosition(0, -1); break;
					case Key.Down: SetWindowPosition(0, 1); break;
					case Key.Left: SetWindowPosition(-1, 0); break;
					case Key.Right: SetWindowPosition(1, 0); break;
					default: e.Handled = false; break;
				}
			}

			base.OnPreviewKeyDown(e);
		}

		void SetWindowPosition(int xOfs, int yOfs)
		{
			Left += xOfs * ((Owner.Width - Width) / 4);
			Top += yOfs * ((Owner.Height - Height) / 4);
		}

		ControlTemplate GetTemplate()
		{
			var template = new ControlTemplate(typeof(EnhancedWindow));

			var outerGrid = new FrameworkElementFactory(typeof(Grid));

			var rect = new FrameworkElementFactory(typeof(Rectangle)) { Name = "PART_rect" };
			rect.SetValue(Rectangle.FillProperty, Brushes.Black);
			rect.SetValue(Rectangle.VisibilityProperty, Visibility.Hidden);
			outerGrid.AppendChild(rect);

			var outerBorder = new FrameworkElementFactory(typeof(Border)) { Name = "blackBorder" };
			outerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
			outerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			outerBorder.SetValue(Border.BackgroundProperty, OuterBrush);
			outerBorder.SetValue(Border.BorderBrushProperty, OuterBrush);
			outerBorder.AddHandler(Border.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonDown));
			outerBorder.AddHandler(Border.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonUp));
			outerBorder.AddHandler(Border.MouseMoveEvent, new MouseEventHandler(OnBorderMouseMove));

			var innerBorder = new FrameworkElementFactory(typeof(Border)) { Name = "innerBorder" };
			innerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
			innerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			innerBorder.SetValue(Border.BackgroundProperty, BackgroundBrush);
			innerBorder.SetValue(Border.BorderBrushProperty, ActiveBrush);

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
			var icon = new FrameworkElementFactory(typeof(Image)) { Name = "enhancedWindowIcon" };
			icon.SetValue(Image.StretchProperty, Stretch.None);
			icon.SetValue(Image.SourceProperty, iconImage);
			icon.SetValue(Image.MarginProperty, new Thickness(5, 0, 0, 0));
			icon.SetValue(Grid.RowProperty, 0);
			icon.SetValue(Grid.ColumnProperty, 0);
			grid.AppendChild(icon);

			var textBlock = new FrameworkElementFactory(typeof(TextBlock)) { Name = "enhancedWindowTitle" };
			textBlock.SetValue(TextBlock.FontSizeProperty, 14d);
			textBlock.SetValue(TextBlock.ForegroundProperty, ActiveBrush);
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

			var minimizeButton = new FrameworkElementFactory(typeof(Button)) { Name = "enhancedWindowMinimize" };
			minimizeButton.SetValue(Button.ContentProperty, "🗕");
			minimizeButton.SetValue(Button.FontSizeProperty, 14d);
			minimizeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			minimizeButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			minimizeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			minimizeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			minimizeButton.SetValue(Button.FocusableProperty, false);
			minimizeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnMinimizeClick));
			stackPanel.AppendChild(minimizeButton);

			var shrinkButton = new FrameworkElementFactory(typeof(Button)) { Name = "shrinkButton" };
			shrinkButton.SetValue(Button.ContentProperty, "🗗");
			shrinkButton.SetValue(Button.FontSizeProperty, 14d);
			shrinkButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			shrinkButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			shrinkButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			shrinkButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			shrinkButton.SetValue(Button.FocusableProperty, false);
			shrinkButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnShrinkClick));
			stackPanel.AppendChild(shrinkButton);

			var growButton = new FrameworkElementFactory(typeof(Button)) { Name = "growButton" };
			growButton.SetValue(Button.ContentProperty, "🗖");
			growButton.SetValue(Button.FontSizeProperty, 14d);
			growButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			growButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			growButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			growButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			growButton.SetValue(Button.FocusableProperty, false);
			growButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnGrowClick));
			stackPanel.AppendChild(growButton);

			var closeButton = new FrameworkElementFactory(typeof(Button)) { Name = "enhancedWindowClose" };
			closeButton.SetValue(Button.ContentProperty, "🗙");
			closeButton.SetValue(Button.FontSizeProperty, 14d);
			closeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			closeButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			closeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			closeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			closeButton.SetValue(Button.FocusableProperty, false);
			closeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnCloseClick));

			stackPanel.AppendChild(closeButton);
			grid.AppendChild(stackPanel);

			var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
			contentPresenter.SetValue(ContentPresenter.FocusVisualStyleProperty, null);
			contentPresenter.SetValue(Grid.RowProperty, 1);
			contentPresenter.SetValue(Grid.ColumnProperty, 0);
			contentPresenter.SetValue(Grid.ColumnSpanProperty, 3);
			grid.AppendChild(contentPresenter);

			innerBorder.AppendChild(grid);
			outerBorder.AppendChild(innerBorder);
			outerGrid.AppendChild(outerBorder);

			template.VisualTree = outerGrid;

			var windowStateTrigger = new Trigger { Property = UIHelper<EnhancedWindow>.GetProperty(x => x.IsFullScreen), Value = true };
			windowStateTrigger.Setters.Add(new Setter { TargetName = outerBorder.Name, Property = Border.BorderThicknessProperty, Value = new Thickness(0) });
			windowStateTrigger.Setters.Add(new Setter { TargetName = rect.Name, Property = VisibilityProperty, Value = Visibility.Visible });
			template.Triggers.Add(windowStateTrigger);

			var isMainWindowTrigger = new Trigger { Property = UIHelper<EnhancedWindow>.GetProperty(x => IsMainWindow), Value = false };
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = icon.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = minimizeButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = shrinkButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = growButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			template.Triggers.Add(isMainWindowTrigger);

			var isActiveTrigger = new Trigger { Property = IsActiveProperty, Value = false };
			isActiveTrigger.Setters.Add(new Setter { TargetName = innerBorder.Name, Property = Border.BorderBrushProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = textBlock.Name, Property = TextBlock.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = minimizeButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = shrinkButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = growButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = closeButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			template.Triggers.Add(isActiveTrigger);

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
			if (IsFullScreen)
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
			{
				if (IsFullScreen)
					OnShrinkClick(null, null);
				else
					OnGrowClick(null, null);
			}
			else if (IsFullScreen)
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
				SetNonFullScreen();
				var dist = PointToScreen(savePoint) - startPos;
				Left = startPos.X - ActualWidth / 2;
				Top -= dist.Y;
				DragMove();
			}
			e.Handled = true;
		}

		void SetNonFullScreen()
		{
			if (!IsFullScreen)
				return;

			var monitor = GetMainMonitor();
			nonFullScreenRect.Width = Math.Max(MinWindowSize, Math.Min(nonFullScreenRect.Width, monitor.Width));
			nonFullScreenRect.Height = Math.Max(MinWindowSize, Math.Min(nonFullScreenRect.Height, monitor.Height));
			nonFullScreenRect.X = Math.Max(monitor.Left, Math.Min(nonFullScreenRect.Left, monitor.Right - nonFullScreenRect.Width));
			nonFullScreenRect.Y = Math.Max(monitor.Top, Math.Min(nonFullScreenRect.Top, monitor.Bottom - nonFullScreenRect.Height));

			SetPosition(nonFullScreenRect);
			IsFullScreen = false;
		}

		void SetPosition(Rect rect)
		{
			var isFullScreen = IsFullScreen;
			IsFullScreen = false;
			var monitors = GetMonitors();
			Left = Math.Max(monitors.Min(r => r.Left), Math.Min(rect.Left, monitors.Max(r => r.Right - rect.Width)));
			Top = Math.Max(monitors.Min(r => r.Top), Math.Min(rect.Top, monitors.Max(r => r.Bottom - rect.Height)));
			Width = Math.Max(MinWindowSize, rect.Width);
			Height = Math.Max(MinWindowSize, rect.Height);
			IsFullScreen = isFullScreen;
		}

		void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

		void OnShrinkClick(object sender, RoutedEventArgs e)
		{
			if (WindowState == WindowState.Minimized)
				return;

			if (!IsFullScreen)
			{
				WindowState = WindowState.Minimized;
				return;
			}

			var size = new Size(Width, Height);
			var fullScreenRects = GetFullScreenRects();
			var index = fullScreenRects.FindIndex(x => x.Item1 == size) - 1;

			if (index < 0)
			{
				SetNonFullScreen();
				return;
			}

			var center = new Point(nonFullScreenRect.Left + nonFullScreenRect.Width / 2, nonFullScreenRect.Top + nonFullScreenRect.Height / 2);
			var newRect = fullScreenRects[index].Item2.OrderBy(x => (new Point(x.Left + x.Width / 2, x.Top + x.Height / 2) - center).LengthSquared).First();
			SetPosition(newRect);
		}

		void OnGrowClick(object sender, RoutedEventArgs e)
		{
			if (WindowState == WindowState.Minimized)
			{
				WindowState = WindowState.Normal;
				return;
			}

			var fullScreenRects = GetFullScreenRects();
			var index = 0;
			if (IsFullScreen)
			{
				var size = new Size(Width, Height);
				index = Math.Min(fullScreenRects.FindIndex(x => x.Item1 == size) + 1, fullScreenRects.Count - 1);
			}
			else
			{
				nonFullScreenRect = new Rect(Left, Top, Width, Height);
				IsFullScreen = true;
			}

			var center = new Point(nonFullScreenRect.Left + nonFullScreenRect.Width / 2, nonFullScreenRect.Top + nonFullScreenRect.Height / 2);
			var newRect = fullScreenRects[index].Item2.OrderBy(x => (new Point(x.Left + x.Width / 2, x.Top + x.Height / 2) - center).LengthSquared).First();
			SetPosition(newRect);
		}

		void OnCloseClick(object sender, RoutedEventArgs e) => Close();

		static void QueueEscape()
		{
			var inputs = new Win32.INPUT[1] { new Win32.INPUT { type = Win32.InputType.KEYBOARD, ki = new Win32.INPUT.KEYBDINPUT { wVk = 69, dwFlags = Win32.KEYEVENTF.KEYUP } } };
			Win32.SendInput(inputs.Length, inputs, Win32.INPUT.Size);
		}

		IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
		{
			if (code >= 0)
			{
				var kbd = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
				if (kbd.vkCode == 91)
					winDown = (Win32.Message)wParam == Win32.Message.WM_KEYDOWN;
				if ((kbd.vkCode == 38) && (winDown) && ((Win32.Message)wParam == Win32.Message.WM_KEYDOWN))
				{
					QueueEscape();
					OnGrowClick(null, null);
					return (IntPtr)1;
				}
				if ((kbd.vkCode == 40) && (winDown) && ((Win32.Message)wParam == Win32.Message.WM_KEYDOWN))
				{
					QueueEscape();
					OnShrinkClick(null, null);
					return (IntPtr)1;
				}
			}

			return Win32.CallNextHookEx(hook, code, wParam, lParam);
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			if (WindowState == WindowState.Maximized)
				WindowState = WindowState.Normal;
		}

		Point lastLocation;
		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);

			var location = new Point(Left, Top);
			if (IsFullScreen)
				nonFullScreenRect.Offset(location - lastLocation);
			lastLocation = location;
		}

		protected override void OnActivated(EventArgs e)
		{
			winDown = false;
			if (!Helpers.IsDebugBuild)
			{
				using (var process = Process.GetCurrentProcess())
				using (var module = process.MainModule)
					hook = Win32.SetWindowsHookEx(Win32.HookType.WH_KEYBOARD_LL, hookProc, Win32.GetModuleHandle(module.ModuleName), 0);
			}
			base.OnActivated(e);
		}

		protected override void OnDeactivated(EventArgs e)
		{
			if (hook != IntPtr.Zero)
			{
				Win32.UnhookWindowsHookEx(hook);
				hook = IntPtr.Zero;
			}
			base.OnDeactivated(e);
		}

		class ScreenPosition
		{
			public Rect Position { get; set; }
			public Rect NonFullScreenPosition { get; set; }
			public bool IsFullScreen { get; set; }

			public override string ToString() => JsonConvert.SerializeObject(this);
			public static ScreenPosition FromString(string str) => JsonConvert.DeserializeObject<ScreenPosition>(str);
		}

		public string GetPosition() => new ScreenPosition { Position = new Rect(Left, Top, Width, Height), NonFullScreenPosition = nonFullScreenRect, IsFullScreen = IsFullScreen }.ToString();

		public void SetPosition(string position)
		{
			var screenPosition = ScreenPosition.FromString(position);
			WindowState = WindowState.Normal;
			SetPosition(screenPosition.Position);
			nonFullScreenRect = screenPosition.NonFullScreenPosition;
			IsFullScreen = screenPosition.IsFullScreen;
		}

		static class Win32
		{
			[DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			struct POINT
			{
				public int X, Y;
				public static POINT FromPoint(Point point) => new POINT { X = (int)(point.X + 0.5), Y = (int)(point.Y + 0.5) };
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
			public class MONITORINFO
			{
				public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
				public RECT rcMonitor = new RECT();
				public RECT rcWork = new RECT();
				public int dwFlags = 0;
			}

			[Serializable]
			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int Left, Top, Right, Bottom;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct KBDLLHOOKSTRUCT
			{
				public int vkCode;
				public int scanCode;
				public int flags;
				public int time;
				public IntPtr dwExtraInfo;
			}

			public enum InputType : uint
			{
				KEYBOARD = 1,
			}

			[Flags]
			public enum KEYEVENTF : uint
			{
				EXTENDEDKEY = 0x0001,
				KEYUP = 0x0002,
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct INPUT
			{
				public InputType type;
				[StructLayout(LayoutKind.Sequential)]
				public struct KEYBDINPUT
				{
					public short wVk;
					public short wScan;
					public KEYEVENTF dwFlags;
					public int time;
					public UIntPtr dwExtraInfo;
				}
				public KEYBDINPUT ki;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
				public byte[] padding;

				public static int Size
				{
					get { return Marshal.SizeOf(typeof(INPUT)); }
				}
			}

			public enum HookType : int
			{
				WH_JOURNALRECORD = 0,
				WH_JOURNALPLAYBACK = 1,
				WH_KEYBOARD = 2,
				WH_GETMESSAGE = 3,
				WH_CALLWNDPROC = 4,
				WH_CBT = 5,
				WH_SYSMSGFILTER = 6,
				WH_MOUSE = 7,
				WH_HARDWARE = 8,
				WH_DEBUG = 9,
				WH_SHELL = 10,
				WH_FOREGROUNDIDLE = 11,
				WH_CALLWNDPROCRET = 12,
				WH_KEYBOARD_LL = 13,
				WH_MOUSE_LL = 14
			}

			public enum Message
			{
				WM_KEYDOWN = 0x0100,
				WM_KEYUP = 0x0101,
			}

			public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
			public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

			[DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);
			[DllImport("user32.dll", SetLastError = true)] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
			[DllImport("user32.dll", SetLastError = true)] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
			[DllImport("kernel32.dll", SetLastError = false)] public static extern IntPtr GetModuleHandle(string lpModuleName);
			[DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(int nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
			[DllImport("user32.dll", SetLastError = true)] public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
		}
	}
}
