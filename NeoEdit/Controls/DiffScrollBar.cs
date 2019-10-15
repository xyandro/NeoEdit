using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace NeoEdit.Program.Controls
{
	public class DiffScrollBar : Grid
	{
		[DepProp(BindsTwoWayByDefault = true)]
		public double Value { get { return UIHelper<DiffScrollBar>.GetPropValue<double>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, GetValidValue(value)); } }
		[DepProp]
		public double ViewportSize { get { return UIHelper<DiffScrollBar>.GetPropValue<double>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, value); } }
		[DepProp]
		public double Minimum { get { return UIHelper<DiffScrollBar>.GetPropValue<double>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, value); } }
		[DepProp]
		public double Maximum { get { return UIHelper<DiffScrollBar>.GetPropValue<double>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, value); } }
		[DepProp]
		public double SmallChange { get { return UIHelper<DiffScrollBar>.GetPropValue<double>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, value); } }
		[DepProp(Default = Orientation.Vertical)]
		public Orientation Orientation { get { return UIHelper<DiffScrollBar>.GetPropValue<Orientation>(this); } set { UIHelper<DiffScrollBar>.SetPropValue(this, value); } }

		public event RoutedPropertyChangedEventHandler<double> ValueChanged;

		static readonly Brush ForegroundBrush = Brushes.White;
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
		static readonly Brush DiffBrush = new SolidColorBrush(Color.FromRgb(120, 102, 3));
		static readonly Brush SliderBrush = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
		static readonly Pen SliderPen = new Pen(new SolidColorBrush(Color.FromRgb(192, 192, 192)), 2);

		List<Tuple<double, double>> diffList;
		public List<Tuple<double, double>> DiffList { get => diffList; set { diffList = value; Invalidate(); } }

		RenderCanvas canvas;

		static DiffScrollBar()
		{
			UIHelper<DiffScrollBar>.Register();
			UIHelper<DiffScrollBar>.AddCallback(x => x.Value, (obj, o, n) => { obj.Invalidate(); obj.ValueChanged?.Invoke(obj, new RoutedPropertyChangedEventArgs<double>(o, n)); });
			UIHelper<DiffScrollBar>.AddCallback(x => x.ViewportSize, (obj, o, n) => obj.Invalidate());
			UIHelper<DiffScrollBar>.AddCallback(x => x.Minimum, (obj, o, n) => obj.Invalidate());
			UIHelper<DiffScrollBar>.AddCallback(x => x.Maximum, (obj, o, n) => obj.Invalidate());
			UIHelper<DiffScrollBar>.AddCallback(x => x.Maximum, (obj, o, n) => obj.Invalidate());
			UIHelper<DiffScrollBar>.AddCallback(x => x.Orientation, (obj, o, n) => obj.DoLayout());
			UIHelper<DiffScrollBar>.AddCoerce(x => x.Value, (obj, value) => obj.GetValidValue(value));

			ForegroundBrush.Freeze();
			BackgroundBrush.Freeze();
			DiffBrush.Freeze();
			SliderBrush.Freeze();
			SliderPen.Freeze();
		}

		public DiffScrollBar()
		{
			DoLayout();
		}

		void Invalidate()
		{
			canvas?.InvalidateVisual();
		}

		double GetValidValue(double value) => Math.Max(Minimum, Math.Min(value, Maximum));

		void DoLayout()
		{
			Children.Clear();
			RowDefinitions.Clear();
			ColumnDefinitions.Clear();

			switch (Orientation)
			{
				case Orientation.Horizontal:
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

					RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

					var leftButton = new RepeatButton { Content = "⮜", Delay = 500, Interval = 50, Foreground = ForegroundBrush, Background = BackgroundBrush, BorderBrush = BackgroundBrush };
					leftButton.Click += OnUpLeftButtonClick;
					SetColumn(leftButton, 0);
					SetRow(leftButton, 0);
					Children.Add(leftButton);

					var rightButton = new RepeatButton { Content = "⮞", Delay = 500, Interval = 50, Foreground = ForegroundBrush, Background = BackgroundBrush, BorderBrush = BackgroundBrush };
					rightButton.Click += OnDownRightButtonClick;
					SetColumn(rightButton, 2);
					SetRow(rightButton, 0);
					Children.Add(rightButton);

					canvas = new RenderCanvas { Background = BackgroundBrush };
					SetColumn(canvas, 1);
					SetRow(canvas, 0);
					Children.Add(canvas);
					break;
				case Orientation.Vertical:
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

					var upButton = new RepeatButton { Content = "⮝", Delay = 500, Interval = 50, Foreground = ForegroundBrush, Background = BackgroundBrush, BorderBrush = BackgroundBrush };
					upButton.Click += OnUpLeftButtonClick;
					SetRow(upButton, 0);
					SetColumn(upButton, 0);
					Children.Add(upButton);

					var downButton = new RepeatButton { Content = "⮟", Delay = 500, Interval = 50, Foreground = ForegroundBrush, Background = BackgroundBrush, BorderBrush = BackgroundBrush };
					downButton.Click += OnDownRightButtonClick;
					SetRow(downButton, 2);
					SetColumn(downButton, 0);
					Children.Add(downButton);

					canvas = new RenderCanvas { Background = BackgroundBrush };
					SetRow(canvas, 1);
					SetColumn(canvas, 0);
					Children.Add(canvas);
					break;
			}

			canvas.Render += OnCanvasRender;
			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
		}

		double ScrollToActual(double scroll)
		{
			switch (Orientation)
			{
				case Orientation.Horizontal: return scroll / (Maximum + ViewportSize) * canvas.ActualWidth;
				case Orientation.Vertical: return scroll / (Maximum + ViewportSize) * canvas.ActualHeight;
				default: throw new Exception("Invalid orientation");
			}

		}

		double ActualToScroll(double actual)
		{
			switch (Orientation)
			{
				case Orientation.Horizontal: return actual / canvas.ActualWidth * (Maximum + ViewportSize);
				case Orientation.Vertical: return actual / canvas.ActualHeight * (Maximum + ViewportSize);
				default: throw new Exception("Invalid orientation");
			}
		}

		Rect GetRect(double value, double size)
		{
			switch (Orientation)
			{
				case Orientation.Horizontal: return new Rect(value, 0, size, canvas.ActualHeight);
				case Orientation.Vertical: return new Rect(0, value, canvas.ActualWidth, size);
				default: throw new Exception("Invalid orientation");
			}
		}

		void OnCanvasRender(object s, DrawingContext dc)
		{
			if (diffList != null)
				foreach (var tuple in diffList)
					dc.DrawRectangle(DiffBrush, null, GetRect(ScrollToActual(tuple.Item1), Math.Max(1, ScrollToActual(tuple.Item2 - tuple.Item1))));

			if (Maximum > Minimum)
			{
				var value = ScrollToActual(Value);
				var size = ScrollToActual(ViewportSize);
				if (size < 5)
				{
					value -= (5 - size) / 2;
					size = 5;
				}
				dc.DrawRoundedRectangle(SliderBrush, SliderPen, GetRect(value, size), 4, 4);
			}
		}

		void OnUpLeftButtonClick(object sender, RoutedEventArgs e) => Value -= SmallChange;

		void OnDownRightButtonClick(object sender, RoutedEventArgs e) => Value += SmallChange;

		double MouseValue(MouseEventArgs e)
		{
			switch (Orientation)
			{
				case Orientation.Horizontal: return e.GetPosition(canvas).X;
				case Orientation.Vertical: return e.GetPosition(canvas).Y;
				default: throw new Exception("Invalid orientation");
			}
		}

		double startValue;
		double startNewValue;

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var newValue = ActualToScroll(MouseValue(e));
			if ((newValue < Value) || (newValue > Value + ViewportSize))
				Value = newValue - ViewportSize / 2;
			startValue = Value;
			startNewValue = newValue;
			canvas.CaptureMouse();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			var newValue = ActualToScroll(MouseValue(e));
			Value = startValue + newValue - startNewValue;
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (canvas.IsMouseCaptured)
				canvas.ReleaseMouseCapture();
			e.Handled = true;
		}
	}
}
