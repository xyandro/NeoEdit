﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		[DepProp(Default = "")]
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
			command.Foreground = CommandMode ? Brushes.Black : Brushes.Gray;
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

		void GuessCommand(bool fullPath)
		{
			var commands = ParseCommand();
			var selPos = command.CaretIndex;
			var selCommand = -1;
			for (var ctr = 0; ctr < commands.Count; ++ctr)
				if ((selPos >= commands[ctr].Item2) && (selPos <= commands[ctr].Item2 + commands[ctr].Item3))
					selCommand = ctr;
			if (selCommand == -1)
				return;

			var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			var dir = Path.GetDirectoryName(commands[selCommand].Item1);
			if ((!String.IsNullOrEmpty(dir)) && (Directory.Exists(dir)))
				foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
					entries[entry] = entry;

			if (selCommand == 0)
			{
				var keepExt = new HashSet<string> { ".exe", ".com", ".bat" };
				var paths = Environment.GetEnvironmentVariable("path").Split(';').Select(path => path.Trim()).Where(path => !String.IsNullOrWhiteSpace(path)).Distinct().ToList();
				foreach (var path in paths)
				{
					if (!Directory.Exists(path))
						continue;

					var files = Directory.EnumerateFiles(path).Where(file => keepExt.Contains(Path.GetExtension(file).ToLower())).ToList();
					foreach (var file in files)
						entries[Path.GetFileName(file)] = file;
				}

				if ((!String.IsNullOrEmpty(Location)) && (Directory.Exists(Location)))
				{
					foreach (var entry in Directory.EnumerateDirectories(Location))
						entries[Path.GetFileName(entry)] = entry;
					foreach (var entry in Directory.EnumerateFiles(Location).Where(file => keepExt.Contains(Path.GetExtension(file).ToLower())))
						entries[Path.GetFileName(entry)] = entry;
				}
			}

			// Limit to those in common with our command
			entries = entries.Where(entry => entry.Key.StartsWith(commands[selCommand].Item1, StringComparison.OrdinalIgnoreCase)).ToDictionary(a => a.Key, a => a.Value);

			var common = entries.Keys.FirstOrDefault() ?? commands[selCommand].Item1;
			foreach (var entry in entries)
			{
				var len = Math.Min(entry.Key.Length, common.Length);
				int ctr;
				for (ctr = 0; ctr < len; ++ctr)
					if (Char.ToLower(entry.Key[ctr]) != Char.ToLower(common[ctr]))
						break;
				common = common.Substring(0, ctr);
			}

			if ((fullPath) || (common != commands[selCommand].Item1))
			{
				if ((fullPath) && (entries.ContainsKey(common)))
					common = entries[common];
				if (common.IndexOfAny(new char[] { '"', ' ' }) != -1)
					common = "\"" + common.Replace("\"", "\"\"") + "\"";
				Command = Command.Substring(0, commands[selCommand].Item2) + common + Command.Substring(commands[selCommand].Item2 + commands[selCommand].Item3);
				command.CaretIndex = commands[selCommand].Item2 + common.Length;
				return;
			}

			var display = entries.Keys.Take(50).ToList();
			if (entries.Count != display.Count)
				display.Add(String.Format("+ {0} more", entries.Count - display.Count));

			Lines.Add(new Line(Line.LineType.Command).Finish());
			Lines.Add(new Line(String.Format("Completions for {0}:", commands[selCommand].Item1), Line.LineType.Command).Finish());
			foreach (var entry in display)
				Lines.Add(new Line(entry, Line.LineType.Command).Finish());
		}

		void CommandKeyDown(object sender, KeyEventArgs e)
		{
			base.OnKeyDown(e);

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter: if (controlDown) GuessCommand(true); else RunCommand(); break;
				case Key.Tab: GuessCommand(false); break;
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

		List<Tuple<string, int, int>> ParseCommand()
		{
			var result = new List<Tuple<string, int, int>>();
			var current = 0;
			var index = 0;
			while (index < Command.Length)
			{
				var c = Command[index++];
				if (Char.IsWhiteSpace(c))
				{
					if (current < result.Count)
						++current;
					continue;
				}

				while (result.Count <= current)
					result.Add(new Tuple<string, int, int>("", index - 1, 0));

				string add;
				if (c == '"')
				{
					var endIndex = index;
					while (true)
					{
						endIndex = Command.IndexOf('"', endIndex) + 1;
						if (endIndex == 0)
							endIndex = Command.Length + 1;
						else if ((Command.Length > endIndex) && (Command[endIndex] == '"'))
						{
							++endIndex;
							continue;
						}
						break;
					}
					add = Command.Substring(index, endIndex - index - 1).Replace("\"\"", "\"");
					index = Math.Min(Command.Length, endIndex);
				}
				else
					add = new String(c, 1);

				result[current] = new Tuple<string, int, int>(result[current].Item1 + add, result[current].Item2, index - result[current].Item2);
			}
			return result;
		}

		ConsoleRunnerPipe pipe = null;
		void RunCommand()
		{
			var commands = ParseCommand();
			if (commands.Count == 0)
				return;

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
				proc.StartInfo.Arguments = "multi consolerunner " + pipeName + " " + String.Join(" ", commands.Select(command => "\"" + command.Item1.Replace("\"", "\"\"") + "\""));
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
