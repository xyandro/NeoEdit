using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
			var line = CreateOrGetAndRemoveLastUnfinished(Line.LineType.Command);
			Lines.Add(new Line(String.Format(@"{0}> {1}", Location, command), Line.LineType.Command));
		}

		string command = "";
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);

			if (pipe != null)
			{
				pipe.Send(ConsoleRunnerPipe.Type.StdIn, Encoding.ASCII.GetBytes(e.Text));
				e.Handled = true;
				return;
			}

			command += e.Text;
			Prompt();
			e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (pipe != null)
			{
				ConsoleKey? key = null;
				switch (e.Key)
				{
					case Key.Return: key = ConsoleKey.Enter; break;
				}

				if (key.HasValue)
				{
					pipe.Send(ConsoleRunnerPipe.Type.StdIn, new byte[] { (byte)key.Value });
					e.Handled = true;
					return;
				}
			}


			switch (e.Key)
			{
				case Key.Enter: RunCommand(); e.Handled = true; break;
				case Key.C: if ((pipe != null) && ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)) pipe.Kill(); break;
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

		ConsoleRunnerPipe pipe = null;
		void RunCommand()
		{
			Lines.Add(CreateOrGetAndRemoveLastUnfinished(Line.LineType.Command).Finish());

			var pipeName = @"\\.\NeoEdit-Console-" + Guid.NewGuid().ToString();
			command = @"C:\Documents\Cpp\NeoEdit - Work\Debug\Test2.exe";

			pipe = new ConsoleRunnerPipe(pipeName, true);
			pipe.Read += DataReceived;
			var name = Environment.GetCommandLineArgs()[0];
#if DEBUG
			name = name.Replace(".vshost.", ".");
#endif
			using (var proc = new Process())
			{
				proc.StartInfo.FileName = name;
				proc.StartInfo.Arguments = "consolerunner " + pipeName;
				proc.Start();
			}
			pipe.Accept();

			command = "";
		}

		void DataReceived(ConsoleRunnerPipe.Type pipeType, byte[] data)
		{
			Dispatcher.Invoke(() =>
			{
				if (pipeType == ConsoleRunnerPipe.Type.None)
				{
					Exited();
					return;
				}

				var type = pipeType == ConsoleRunnerPipe.Type.StdOut ? Line.LineType.StdOut : Line.LineType.StdErr;

				var str = Encoding.ASCII.GetString(data);
				str = str.Replace("\r", "");
				var index = 0;
				while (index < str.Length)
				{
					var endIndex = str.IndexOf('\n', index);
					var newline = endIndex != -1;
					if (!newline)
						endIndex = str.Length;

					var line = CreateOrGetAndRemoveLastUnfinished(type);
					line = line + str.Substring(index, endIndex - index);
					if (newline)
						line = line.Finish();
					Lines.Add(line);

					index = endIndex + (newline ? 1 : 0);
				}
			});
		}

		void Exited()
		{
			pipe.Dispose();
			pipe = null;

			Dispatcher.Invoke(() =>
			{
				FinishAll();
				Prompt();
			});
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
