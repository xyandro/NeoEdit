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
		string Command { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp(Default = true)]
		bool CommandMode { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
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
			UIHelper<Console>.AddCallback(a => a.CommandMode, (obj, s, e) => obj.SetFocus());
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
			canvas.KeyDown += CanvasKeyDown;
			canvas.TextInput += CanvasTextInput;
			canvas.MouseLeftButtonDown += (s, e) => canvas.Focus();

			command.KeyDown += CommandKeyDown;

			Loaded += (s, e) => SetFocus();
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

		void SetFocus()
		{
			command.IsReadOnly = !CommandMode;
			if (CommandMode)
				command.Focus();
			else
				canvas.Focus();
		}

		void CanvasTextInput(object sender, TextCompositionEventArgs e)
		{
			if (pipe == null)
				return;

			pipe.Send(ConsoleRunnerPipe.Type.StdIn, Encoding.ASCII.GetBytes(e.Text));
			e.Handled = true;
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		void CanvasKeyDown(object sender, KeyEventArgs e)
		{
			if (pipe == null)
				return;

			ConsoleKey? sendKey = null;
			switch (e.Key)
			{
				case Key.Return: sendKey = ConsoleKey.Enter; break;
				case Key.C: if (controlDown) pipe.Kill(); break;
			}

			if (!sendKey.HasValue)
				return;

			pipe.Send(ConsoleRunnerPipe.Type.StdIn, new byte[] { (byte)sendKey.Value });
			e.Handled = true;
		}

		void CommandKeyDown(object sender, KeyEventArgs e)
		{
			base.OnKeyDown(e);

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter: RunCommand(); break;
				default: e.Handled = false; break;
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
			Command = @"C:\Documents\Cpp\NeoEdit - Work\Debug\Test2.exe";
			Lines.Add(new Line(String.Format("Starting program {0}.", Command), Line.LineType.Command).Finish());
			Lines.Add(new Line(Line.LineType.Command).Finish());

			var pipeName = @"\\.\NeoEdit-Console-" + Guid.NewGuid().ToString();

			pipe = new ConsoleRunnerPipe(pipeName, true);
			pipe.Read += DataReceived;
			var name = Environment.GetCommandLineArgs()[0];
#if DEBUG
			name = name.Replace(".vshost.", ".");
#endif
			using (var proc = new Process())
			{
				proc.StartInfo.FileName = name;
				proc.StartInfo.Arguments = "multi consolerunner " + pipeName;
				proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				proc.Start();
			}
			pipe.Accept();

			CommandMode = false;
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
			CommandMode = true;
			Command = "";

			Dispatcher.Invoke(() =>
			{
				FinishAll();
				Lines.Add(new Line(Line.LineType.Command).Finish());
				Lines.Add(new Line("Program completed.", Line.LineType.Command).Finish());
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
