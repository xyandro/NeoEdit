using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
			UIHelper<Console>.AddObservableCallback(a => a.Lines, (obj, s, e) => obj.renderTimer.Start());
			UIHelper<Console>.AddCallback(a => a.yScrollValue, (obj, s, e) => obj.renderTimer.Start());
			UIHelper<Console>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		Font font = new Font();
		RunOnceTimer renderTimer;

		readonly UIHelper<Console> uiHelper;
		public Console(string path = null)
		{
			uiHelper = new UIHelper<Console>(this);
			InitializeComponent();

			renderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			UIHelper<Canvas>.AddCallback(canvas, Canvas.ActualHeightProperty, () => CalculateBoundaries());
			UIHelper<Canvas>.AddCallback(canvas, Canvas.ActualWidthProperty, () => CalculateBoundaries());

			Location = path;
			if (Location == null)
				Location = Directory.GetCurrentDirectory();

			Lines = new ObservableCollection<Line>();

			canvas.Render += OnCanvasRender;

			Prompt();
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			yScroll.ViewportSize = canvas.ActualHeight / font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Lines.Count - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			renderTimer.Start();
		}

		void Prompt()
		{
			FinishAll();

			var line = CreateOrGetAndRemoveLastUnfinished(Line.LineType.Command);
			Lines.Add(line + String.Format(@"{0}> ", Location));
		}

		string command = "";
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);

			if (proc != null)
			{
				proc.Write(e.Text);
				e.Handled = true;
				return;
			}

			command += e.Text;
			Lines[Lines.Count - 1] += e.Text;
			e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (proc != null)
			{
				var done = true;
				switch (e.Key)
				{
					case Key.Return: break;
					default: done = false; break;
				}

				if (done)
				{
					proc.Write((byte)e.Key);
					e.Handled = true;
					return;
				}
			}


			switch (e.Key)
			{
				case Key.Enter: RunCommand(); e.Handled = true; break;
				case Key.C: if ((proc != null) && ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)) proc.Kill(); break;
			}
		}

		const int lookBehindLimit = 50;
		Line CreateOrGetAndRemoveLastUnfinished(Line.LineType type)
		{
			var first = Math.Max(0, Lines.Count - lookBehindLimit);
			for (var ctr = Lines.Count - 1; ctr >= first; --ctr)
				if ((Lines[ctr].Type == type) && (!Lines[ctr].Finished))
				{
					var last = Lines[ctr];
					Lines.RemoveAt(ctr);
					return last;
				}

			return new Line(type);
		}

		void FinishAll()
		{
			var first = Math.Max(0, Lines.Count - lookBehindLimit);
			for (var ctr = Lines.Count - 1; ctr >= first; --ctr)
				if (!Lines[ctr].Finished)
					Lines[ctr] = Lines[ctr].Finish();
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
			var y = 0.0;
			for (var ctr = startLine; ctr < endLine; ++ctr)
			{
				var line = Lines[ctr];
				var text = font.GetText(line.Str, brushes[line.Type]);
				dc.DrawText(text, new Point(0, y));
				y += font.lineHeight;
			}
		}
	}
}
