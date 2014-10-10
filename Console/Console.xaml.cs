using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.GUI.Common;

namespace NeoEdit.Console
{
	public partial class Console
	{
		[DepProp]
		string Location { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		ObservableCollection<Line> Lines { get { return uiHelper.GetPropValue<ObservableCollection<Line>>(); } set { uiHelper.SetPropValue(value); } }

		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize); } }

		static Console()
		{
			UIHelper<Console>.Register();
			UIHelper<Console>.AddObservableCallback(a => a.Lines, (obj, s, e) => obj.CalculateBoundaries());
			UIHelper<Console>.AddObservableCallback(a => a.Lines, (obj, s, e) => obj.yScrollValue = (int)obj.yScroll.Maximum);
			UIHelper<Console>.AddObservableCallback(a => a.Lines, (obj, s, e) => obj.InvalidateRender());
			UIHelper<Console>.AddCallback(a => a.yScrollValue, (obj, s, e) => obj.InvalidateRender());
			UIHelper<Console>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		internal readonly double charWidth;
		internal readonly double lineHeight;
		readonly Typeface typeface;
		readonly double fontSize;

		readonly UIHelper<Console> uiHelper;
		public Console(string path = null)
		{
			uiHelper = new UIHelper<Console>(this);
			InitializeComponent();

			UIHelper<Canvas>.AddCallback(canvas, Canvas.ActualHeightProperty, () => CalculateBoundaries());
			UIHelper<Canvas>.AddCallback(canvas, Canvas.ActualWidthProperty, () => CalculateBoundaries());

			Location = path;
			if (Location == null)
				Location = Directory.GetCurrentDirectory();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			Lines = new ObservableCollection<Line>();

			canvas.Render += OnCanvasRender;

			Prompt();
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			yScroll.ViewportSize = canvas.ActualHeight / lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Lines.Count - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			InvalidateRender();
		}

		void Prompt()
		{
			var line = CreateOrGetAndRemoveLastUnfinished(Line.LineType.Command);
			Lines.Add(line + String.Format(@"{0}> ", Location));
		}

		string command = "";
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			command += e.Text;
			Lines[Lines.Count - 1] += e.Text;
			e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (proc != null)
			{
				proc.Write((byte)e.Key);
				e.Handled = true;
				return;
			}

			base.OnKeyDown(e);
			switch (e.Key)
			{
				case Key.Enter: RunCommand(); e.Handled = true; break;
			}
		}

		Line CreateOrGetAndRemoveLastUnfinished(Line.LineType type)
		{
			var last = Lines.LastOrDefault(line => (line.Type == type) && (!line.Finished));
			if (last == null)
				return new Line(type);

			Lines.Remove(last);
			return last;
		}

		AsyncProcess proc = null;
		void RunCommand()
		{
			Lines.Add(CreateOrGetAndRemoveLastUnfinished(Line.LineType.Command).Finish());

			command = @"C:\Documents\Cpp\TestConsole\bin\Debug\TestConsole.exe";
			proc = new AsyncProcess(command);
			proc.Exit += s => Exited();
			proc.StdOutData += (s, d, n) => DataReceived(Line.LineType.StdOut, d, n);
			proc.StdErrData += (s, d, n) => DataReceived(Line.LineType.StdErr, d, n);
			proc.Start();
		}

		void DataReceived(Line.LineType type, string text, bool newline)
		{
			Dispatcher.Invoke(() =>
			{
				var line = CreateOrGetAndRemoveLastUnfinished(type);
				line = line + text;
				if (newline)
					line = line.Finish();
				Lines.Add(line);
			});
		}

		void Exited()
		{
			proc.Dispose();
			proc = null;

			Dispatcher.Invoke(Prompt);
		}

		internal Label GetLabel()
		{
			var label = new Label { Padding = new Thickness(10, 2, 10, 2) };
			label.SetBinding(Label.ContentProperty, new Binding("Location") { Source = this, Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"FileName([0])" });
			return label;
		}

		DispatcherTimer renderTimer = null;
		void InvalidateRender()
		{
			if (renderTimer != null)
				return;

			renderTimer = new DispatcherTimer();
			renderTimer.Tick += (s, e) =>
			{
				renderTimer.Stop();
				renderTimer = null;
				canvas.InvalidateVisual();
			};
			renderTimer.Start();
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			var brushes = new Dictionary<Line.LineType, Brush>
			{
				{ Line.LineType.Command, Misc.CommandBrush },
				{ Line.LineType.StdOut, Misc.OutputBrush },
				{ Line.LineType.StdErr, Misc.ErrorBrush },
			};
			var startLine = yScrollValue;
			var endLine = Math.Min(Lines.Count, startLine + yScrollViewportCeiling);
			var y = 0;
			for (var ctr = startLine; ctr < endLine; ++ctr)
			{
				var line = Lines[ctr];
				var text = new FormattedText(line.Str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, brushes[line.Type]);
				dc.DrawText(text, new Point(0, y));
				y += (int)lineHeight;
			}
		}
	}
}
