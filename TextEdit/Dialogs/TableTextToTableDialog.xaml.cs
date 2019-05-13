using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Misc;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class TableTextToTableDialog
	{
		const double border = 2;

		public class Result
		{
			public List<int> LineBreaks { get; set; }
		}

		List<string> lines;
		HashSet<int> lineBreaks;

		static TableTextToTableDialog() { UIHelper<TableTextToTableDialog>.Register(); }

		TableTextToTableDialog(string text)
		{
			InitializeComponent();

			AutoSetup(text);
			SetupCanvas();
		}

		void AutoSetup(string text)
		{
			lines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Select(line => line.TrimEnd()).NonNullOrEmpty().ToList();
			if (!lines.NonNullOrWhiteSpace().Any())
				throw new Exception("No data!");

			var longest = lines.Max(line => line.Length);
			var shouldBreak = new bool[longest + 1];
			for (var ctr = 0; ctr < shouldBreak.Length; ++ctr)
				shouldBreak[ctr] = true;

			foreach (var line in lines)
				for (var pos = 0; pos < line.Length; ++pos)
					if (shouldBreak[pos + 1])
						if (!char.IsWhiteSpace(line[pos]))
							shouldBreak[pos + 1] = false;

			shouldBreak[0] = shouldBreak[longest] = true;

			for (var ctr = 0; ctr < shouldBreak.Length - 1; ++ctr)
				if ((shouldBreak[ctr]) && (shouldBreak[ctr + 1]))
					shouldBreak[ctr] = false;

			lineBreaks = new HashSet<int>(shouldBreak.Indexes(pos => pos));
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { LineBreaks = lineBreaks.OrderBy().ToList() };
			DialogResult = true;
		}

		void Button_Click(object sender, RoutedEventArgs e)
		{
			if (!lineBreaks.Any())
				return;

			lineBreaks.Remove(lineBreaks.Min());
			SetupCanvas();
		}

		void SetupCanvas()
		{
			canvas.Children.Clear();
			var numChars = lines.Max(line => line.Length);
			canvas.Width = numChars * Font.CharWidth + border * 2;
			canvas.Height = lines.Count * Font.FontSize + border * 2;
			for (var line = 0; line < lines.Count; ++line)
			{
				var textBlock = new TextBlock { Text = lines[line], FontFamily = Font.FontFamily, FontSize = Font.FontSize };
				Canvas.SetLeft(textBlock, border);
				Canvas.SetTop(textBlock, line * Font.FontSize + border);
				canvas.Children.Add(textBlock);
			}

			foreach (var lineBreak in lineBreaks)
			{
				var x = lineBreak * Font.CharWidth + border;
				var line = new Line { X1 = x, Y1 = 0, X2 = x, Y2 = canvas.Height, Stroke = Brushes.Black };
				canvas.Children.Add(line);
			}
		}

		void OnCanvasClick(object sender, MouseButtonEventArgs e)
		{
			var pos = (int)((e.GetPosition(canvas).X - border) / Font.CharWidth + 0.5);
			if (lineBreaks.Contains(pos))
				lineBreaks.Remove(pos);
			else
				lineBreaks.Add(pos);
			SetupCanvas();
		}

		static public Result Run(Window parent, string text)
		{
			var dialog = new TableTextToTableDialog(text) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
