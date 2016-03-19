﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Converters;

namespace NeoEdit.GUI.Controls
{
	partial class NEExpressionResults
	{
		[DepProp]
		public string Expression { get { return UIHelper<NEExpressionResults>.GetPropValue<string>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<NEExpressionResults>.GetPropValue<NEVariables>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public int? NumResults { get { return UIHelper<NEExpressionResults>.GetPropValue<int?>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public int DisplayResults { get { return UIHelper<NEExpressionResults>.GetPropValue<int>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp(BindsTwoWayByDefault = true)]
		public bool IsValid { get { return UIHelper<NEExpressionResults>.GetPropValue<bool>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiRow { get { return UIHelper<NEExpressionResults>.GetPropValue<bool>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public string ErrorMessage { get { return UIHelper<NEExpressionResults>.GetPropValue<string>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }

		static readonly double RowHeight;

		static NEExpressionResults()
		{
			UIHelper<NEExpressionResults>.Register();
			UIHelper<NEExpressionResults>.AddCallback(a => a.Expression, (obj, o, n) => obj.UpdateChildren());
			UIHelper<NEExpressionResults>.AddCallback(a => a.Variables, (obj, o, n) => obj.UpdateChildren());
			UIHelper<NEExpressionResults>.AddCallback(a => a.NumResults, (obj, o, n) => obj.UpdateChildren());
			UIHelper<NEExpressionResults>.AddCallback(a => a.DisplayResults, (obj, o, n) => obj.UpdateChildren());
			UIHelper<NEExpressionResults>.AddCallback(a => a.MultiRow, (obj, o, n) => obj.UpdateChildren());
			RowHeight = CalcRowHeight();
		}

		static double CalcRowHeight()
		{
			var text = new TextBlock { Text = "THE QUICK BROWN FOX JUMPED OVER THE LAZY DOGS the quick brown fox jumped over the lazy dogs" };
			text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return text.DesiredSize.Height;
		}

		public NEExpressionResults()
		{
			InitializeComponent();
			DisplayResults = 10;
			SizeChanged += (s, e) =>
			{
				var displayResults = CountFromHeight;
				if (DisplayResults == displayResults)
					UpdateChildren();
				else
					DisplayResults = displayResults;
			};
		}

		int CountFromHeight => MultiRow ? Math.Max(0, (int)((ActualHeight - Spacing * 2) / RowHeight - 1)) : DisplayResults;

		void AddChild(UIElement element, int row, int column, int rowSpan = 1, int columnSpan = 1, int? index = null)
		{
			if (element == null)
				return;
			Grid.SetColumn(element, column);
			Grid.SetRow(element, row);
			Grid.SetColumnSpan(element, columnSpan);
			Grid.SetRowSpan(element, rowSpan);
			grid.Children.Insert(index ?? grid.Children.Count, element);
		}

		FrameworkElement GetTextBlock(string text, Brush background = null) => new TextBlock { Text = text ?? "ERROR", Foreground = text == null ? Brushes.DarkRed : Brushes.Black, Background = background };

		IEnumerable<FrameworkElement> GetErrorControls()
		{
			var rectangle = new Rectangle { Fill = Brushes.LightGray, Opacity = .90 };
			rectangle.SetBinding(Rectangle.VisibilityProperty, new Binding(nameof(IsValid)) { Source = this, Converter = new NEExpressionConverter(), ConverterParameter = "![0]" });
			yield return rectangle;

			var textBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(ErrorMessage)) { Source = this });
			textBlock.SetBinding(TextBlock.VisibilityProperty, new Binding(nameof(IsValid)) { Source = this, Converter = new NEExpressionConverter(), ConverterParameter = "![0]" });
			yield return textBlock;
		}

		[Flags]
		enum WidthType
		{
			None = 0,
			Expand = 1,
			Shrink1 = 2,
			Shrink2 = 4,
			Shrink3 = 8,
			Shrink4 = 16,
			Shrink5 = 32,
			Shrink6 = 64,
			Shrink7 = 128,
		}

		readonly Brush BackColor = Brushes.LightGray;
		const int Spacing = 2;
		void UpdateChildren()
		{
			List<string> variables;
			List<string> results;
			Dictionary<string, List<string>> varValues;
			try
			{
				var expression = new NEExpression(Expression);
				variables = new List<string>(expression.Variables);
				var showResults = !NumResults.HasValue ? default(int?) : Math.Min(NumResults.Value, DisplayResults);
				var resultCount = Math.Min(Variables.Prepare(expression.Variables, showResults), DisplayResults);
				results = expression.EvaluateRows<string>(Variables, resultCount).Coalesce("").ToList();
				varValues = Variables.GetAllValues().ToDictionary(pair => pair.Key, pair => pair.Value.Select(value => value?.ToString() ?? "").ToList());
				IsValid = true;
				ErrorMessage = null;
			}
			catch (Exception ex)
			{
				results = MultiRow ? Enumerable.Repeat(default(string), DisplayResults).ToList() : new List<string>();
				variables = new List<string>();
				varValues = new Dictionary<string, List<string>>();
				IsValid = false;
				ErrorMessage = ex.Message;
				if ((grid.Children.Count != 0) || (ActualHeight == 0) || (ActualWidth == 0))
					return;
			}

			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();

			Func<WidthType, int, Tuple<WidthType, List<FrameworkElement>>> GetLine = (widthType, numRows) => Tuple.Create(widthType, Enumerable.Range(0, numRows).Select(row => new Rectangle { Width = Spacing, Fill = BackColor }).Cast<FrameworkElement>().ToList());

			var columns = new List<Tuple<WidthType, List<FrameworkElement>>>();

			if (MultiRow)
			{
				Func<WidthType, int, Tuple<WidthType, List<FrameworkElement>>> GetSpace = (widthType, numRows) => Tuple.Create(widthType, new[] { new Rectangle { Width = Spacing, Fill = BackColor } }.Concat(Enumerable.Range(0, numRows - 1).Select(row => new Rectangle { Width = Spacing })).Cast<FrameworkElement>().ToList());
				columns.Add(GetLine(WidthType.None, results.Count + 1));
				columns.AddRange(variables.SelectMany(variable => new[] {
					GetSpace(WidthType.Shrink3, results.Count + 1),
					Tuple.Create(WidthType.Expand | WidthType.Shrink4, new[] { GetTextBlock(variable, BackColor) }.Concat(Enumerable.Range(0, results.Count).Select(result => GetTextBlock(varValues[variable][result]))).ToList()),
					GetSpace(WidthType.Shrink3, results.Count + 1),
					GetLine(WidthType.Shrink5, results.Count + 1),
				}));
				if (!variables.Any())
				{
					columns.Add(GetSpace(WidthType.Shrink3, results.Count + 1));
					columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink4, new[] { GetTextBlock("", BackColor) }.Concat(results.Select(result => GetTextBlock("<No vars>"))).ToList()));
					columns.Add(GetSpace(WidthType.Shrink3, results.Count + 1));
					columns.Add(GetLine(WidthType.Shrink5, results.Count + 1));
				}

				columns.Add(Tuple.Create(WidthType.Shrink1, new[] { default(TextBlock) }.Concat(results.Select(result => GetTextBlock(" => "))).ToList()));
				columns.Add(GetLine(WidthType.Shrink2, results.Count + 1));

				columns.Add(Tuple.Create(WidthType.Shrink6, new[] { new Rectangle { Width = Spacing, Fill = BackColor } }.Concat(Enumerable.Repeat(default(FrameworkElement), results.Count)).ToList()));
				columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink7, new[] { GetTextBlock("Result", BackColor) }.Concat(results.Select(result => GetTextBlock(result))).ToList()));
				columns.Add(Tuple.Create(WidthType.Shrink6, new[] { new Rectangle { Width = Spacing, Fill = BackColor } }.Concat(Enumerable.Repeat(default(FrameworkElement), results.Count)).ToList()));

				columns.Add(GetLine(WidthType.None, results.Count + 1));
			}
			else
			{
				Func<WidthType, Tuple<WidthType, List<FrameworkElement>>> GetSpace = widthType => Tuple.Create(widthType, new[] { new Rectangle { Width = Spacing } }.Cast<FrameworkElement>().ToList());
				columns.Add(GetLine(WidthType.None, 1));
				if (results.Count == 0)
				{
					columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink1, new List<FrameworkElement> { default(FrameworkElement) }));
					columns.Add(GetLine(WidthType.None, 1));
				}
				else
					columns.AddRange(results.SelectMany(result => new[] {
						GetSpace(WidthType.Shrink1),
						Tuple.Create(WidthType.Expand | WidthType.Shrink2, new List<FrameworkElement> { GetTextBlock(result) }),
						GetSpace(WidthType.Shrink1),
						GetLine(WidthType.Shrink3, 1),
					}).ToList());
			}

			// Sanity check: should have same number on all columns
			if (columns.Select(column => column.Item2.Count).Distinct().Count() != 1)
				throw new Exception("Column counts don't match");

			// Measure everything
			var margin = new Thickness(0);
			var size = new Size(double.PositiveInfinity, double.PositiveInfinity);
			columns.SelectMany(column => column.Item2).Where(element => element != null).ForEach(element => { element.Margin = margin; element.Measure(size); });
			const int precision = 10000;
			var widths = columns.Select(column => (int)(column.Item2.Max(text => text?.DesiredSize.Width * precision) ?? 0)).ToList();
			var available = Math.Max(0, (int)ActualWidth * precision);

			// Expand as necessary
			var expandIndexes = columns.Indexes(column => column.Item1.HasFlag(WidthType.Expand)).ToList();
			var extraPerColumn = (available - widths.Sum()) / expandIndexes.Count;
			if (extraPerColumn > 0)
				expandIndexes.ForEach(index => widths[index] += extraPerColumn);

			// Shrink as necessary
			var shrinks = new[] { WidthType.Shrink1, WidthType.Shrink2, WidthType.Shrink3, WidthType.Shrink4, WidthType.Shrink5, WidthType.Shrink6, WidthType.Shrink7 };
			foreach (var shrink in shrinks)
			{
				var shrinkIndexes = columns.Indexes(column => column.Item1.HasFlag(shrink)).ToList();
				while ((widths.Sum() > available) && (shrinkIndexes.Any(index => widths[index] != 0)))
				{
					var shrinkWidths = shrinkIndexes.Select(column => widths[column]).OrderByDescending().Distinct().ToList();
					var maxIndexes = shrinkIndexes.Where(column => widths[column] == shrinkWidths[0]).ToList();
					var minWidth = shrinkWidths.Count > 1 ? shrinkWidths[1] : 0;
					var extraPerIndex = (widths.Sum() - available + maxIndexes.Count - 1) / maxIndexes.Count;
					maxIndexes.ForEach(column => widths[column] = Math.Max(minWidth, widths[column] - extraPerIndex));
				}
			}

			var rows = columns[0].Item2.Count;
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Spacing) });
			for (var row = 0; row < rows; ++row)
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Spacing) });

			for (var column = 0; column < columns.Count; ++column)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)widths[column] / precision) });
				for (var row = 0; row < rows; ++row)
					AddChild(columns[column].Item2[row], row + 1, column);
			}

			AddChild(new Rectangle { Fill = BackColor, Height = Spacing }, 0, 0, columnSpan: grid.ColumnDefinitions.Count);
			AddChild(new Rectangle { Fill = BackColor, Height = Spacing }, grid.RowDefinitions.Count - 1, 0, columnSpan: grid.ColumnDefinitions.Count);

			GetErrorControls().ForEach(control => AddChild(control, 0, 0, grid.RowDefinitions.Count, grid.ColumnDefinitions.Count));
		}
	}
}