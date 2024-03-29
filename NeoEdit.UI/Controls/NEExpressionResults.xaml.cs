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
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Converters;

namespace NeoEdit.UI.Controls
{
	partial class NEExpressionResults
	{
		[DepProp]
		public string Expression { get { return UIHelper<NEExpressionResults>.GetPropValue<string>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<NEExpressionResults>.GetPropValue<NEVariables>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public int? RowCount { get { return UIHelper<NEExpressionResults>.GetPropValue<int?>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp(BindsTwoWayByDefault = true)]
		public bool IsValid { get { return UIHelper<NEExpressionResults>.GetPropValue<bool>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public bool MultiRow { get { return UIHelper<NEExpressionResults>.GetPropValue<bool>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public string ErrorMessage { get { return UIHelper<NEExpressionResults>.GetPropValue<string>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }
		[DepProp]
		public string CountExpression { get { return UIHelper<NEExpressionResults>.GetPropValue<string>(this); } set { UIHelper<NEExpressionResults>.SetPropValue(this, value); } }

		static readonly double RowHeight;

		static NEExpressionResults()
		{
			UIHelper<NEExpressionResults>.Register();
			UIHelper<NEExpressionResults>.AddCallback(a => a.Expression, (obj, o, n) => obj.Invalidate());
			UIHelper<NEExpressionResults>.AddCallback(a => a.Variables, (obj, o, n) => obj.Invalidate());
			UIHelper<NEExpressionResults>.AddCallback(a => a.RowCount, (obj, o, n) => obj.Invalidate());
			UIHelper<NEExpressionResults>.AddCallback(a => a.MultiRow, (obj, o, n) => obj.Invalidate());
			UIHelper<NEExpressionResults>.AddCallback(a => a.CountExpression, (obj, o, n) => obj.Invalidate());
			RowHeight = CalcRowHeight();
			LineBrush.Freeze();
			HeaderBrush.Freeze();
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
		}

		void Invalidate()
		{
			InvalidateMeasure();
			UpdateChildren();
		}

		int GetRowCount() => Variables.RowCount(new List<string> { Expression, CountExpression }.NonNullOrWhiteSpace().SelectMany(expr => new NEExpression(expr).VariableUses), RowCount);

		protected override Size MeasureOverride(Size constraint)
		{
			base.MeasureOverride(constraint);
			try
			{
				var rowCount = MultiRow ? Math.Min(GetRowCount(), 10) + 1 : 1;
				return new Size(0, rowCount * RowHeight + Spacing * 2);
			}
			catch { return RenderSize; }
		}

		void AddChild(UIElement element, int row, int column, int rowSpan = 1, int columnSpan = 1, int? index = null)
		{
			if (element == null)
				return;
			Grid.SetColumn(element, column);
			Grid.SetRow(element, row);
			Grid.SetColumnSpan(element, columnSpan);
			Grid.SetRowSpan(element, rowSpan);
			Children.Insert(index ?? Children.Count, element);
		}

		FrameworkElement GetTextBlock(string text, Brush background = null)
		{
			var textBlock = new TextBlock { Text = text ?? "ERROR", Background = background };
			if (text == null)
				textBlock.Foreground = Brushes.DarkRed;
			return textBlock;
		}

		IEnumerable<FrameworkElement> GetErrorControls()
		{
			var rectangle = new Rectangle { RadiusX = 2, RadiusY = 2, Stroke = LineBrush, StrokeThickness = Spacing, Fill = HeaderBrush, Opacity = .90 };
			rectangle.SetBinding(Rectangle.VisibilityProperty, new Binding(nameof(IsValid)) { Source = this, Converter = new NEExpressionConverter(), ConverterParameter = "!p0" });
			yield return rectangle;

			var textBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(ErrorMessage)) { Source = this });
			textBlock.SetBinding(TextBlock.VisibilityProperty, new Binding(nameof(IsValid)) { Source = this, Converter = new NEExpressionConverter(), ConverterParameter = "!p0" });
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

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateChildren();
		}

		static readonly Brush LineBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
		static readonly Brush HeaderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
		const double Spacing = 1;
		void UpdateChildren()
		{
			List<NEVariableUse> variables;
			List<string> results;
			Dictionary<string, List<string>> varValues;
			int rowCount;
			var useResults = MultiRow ? Math.Max(0, (int)((ActualHeight - Spacing * 2) / RowHeight - 1)) : 10;
			try
			{
				var expression = new NEExpression(Expression);
				variables = expression.VariableUses.Distinct(varUse => varUse.Display).ToList();
				rowCount = GetRowCount();
				var useCount = Math.Min(rowCount, useResults);
				results = expression.Evaluate<string>(Variables, 0, useCount, rowCount).Coalesce("").ToList();
				varValues = variables.ToDictionary(variable => variable.Display, variable => Variables.GetValues(variable, useCount, rowCount).Select(val => val?.ToString()).ToList());
				IsValid = true;
				ErrorMessage = null;
			}
			catch (Exception ex)
			{
				results = MultiRow ? Enumerable.Repeat(default(string), useResults).ToList() : new List<string>();
				variables = new List<NEVariableUse>();
				rowCount = 0;
				varValues = new Dictionary<string, List<string>>();
				IsValid = false;
				ErrorMessage = ex.Message;
			}

			Children.Clear();
			ColumnDefinitions.Clear();
			RowDefinitions.Clear();

			Func<WidthType, int, Tuple<WidthType, List<FrameworkElement>>> GetLine = (widthType, numRows) => Tuple.Create(widthType, Enumerable.Range(0, numRows).Select(row => new Rectangle { Width = Spacing, Fill = LineBrush }).Cast<FrameworkElement>().ToList());

			var columns = new List<Tuple<WidthType, List<FrameworkElement>>>();

			if (MultiRow)
			{
				Func<WidthType, int, Tuple<WidthType, List<FrameworkElement>>> GetSpace = (widthType, numRows) => Tuple.Create(widthType, new[] { new Rectangle { Width = Spacing, Fill = HeaderBrush } }.Concat(Enumerable.Range(0, numRows - 1).Select(row => new Rectangle { Width = Spacing })).Cast<FrameworkElement>().ToList());
				columns.AddRange(variables.SelectMany(variableUse => new[] {
					GetSpace(WidthType.Shrink3, results.Count + 1),
					Tuple.Create(WidthType.Expand | WidthType.Shrink4, new[] { GetTextBlock(variableUse.Display, HeaderBrush) }.Concat(Enumerable.Range(0, results.Count).Select(result => GetTextBlock(varValues[variableUse.Display][result]))).ToList()),
					GetSpace(WidthType.Shrink3, results.Count + 1),
					GetLine(WidthType.Shrink5, results.Count + 1),
				}));
				if (!variables.Any())
				{
					columns.Add(GetSpace(WidthType.Shrink3, results.Count + 1));
					columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink4, new[] { GetTextBlock("", HeaderBrush) }.Concat(results.Select(result => GetTextBlock("<No vars>"))).ToList()));
					columns.Add(GetSpace(WidthType.Shrink3, results.Count + 1));
					columns.Add(GetLine(WidthType.Shrink5, results.Count + 1));
				}

				columns.Add(Tuple.Create(WidthType.Shrink1, new[] { default(TextBlock) }.Concat(results.Select(result => GetTextBlock(" => "))).ToList()));
				columns.Add(GetLine(WidthType.Shrink2, results.Count + 1));

				columns.Add(Tuple.Create(WidthType.Shrink6, new[] { new Rectangle { Width = Spacing, Fill = HeaderBrush } }.Concat(Enumerable.Repeat(default(FrameworkElement), results.Count)).ToList()));
				columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink7, new[] { GetTextBlock($"Results ({rowCount})", HeaderBrush) }.Concat(results.Select(result => GetTextBlock(result))).ToList()));
				columns.Add(Tuple.Create(WidthType.Shrink6, new[] { new Rectangle { Width = Spacing, Fill = HeaderBrush } }.Concat(Enumerable.Repeat(default(FrameworkElement), results.Count)).ToList()));
			}
			else
			{
				Func<WidthType, Tuple<WidthType, List<FrameworkElement>>> GetSpace = widthType => Tuple.Create(widthType, new[] { new Rectangle { Width = Spacing } }.Cast<FrameworkElement>().ToList());
				if (results.Count == 0)
					columns.Add(Tuple.Create(WidthType.Expand | WidthType.Shrink1, new List<FrameworkElement> { default(FrameworkElement) }));
				else
					columns.AddRange(results.SelectMany((result, index) => new[] {
						index == 0 ? null : GetLine(WidthType.Shrink3, 1),
						GetSpace(WidthType.Shrink1),
						Tuple.Create(WidthType.Expand | WidthType.Shrink2, new List<FrameworkElement> { GetTextBlock(result) }),
						GetSpace(WidthType.Shrink1),
					}).NonNull().ToList());
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
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(Spacing) });
			for (var row = 0; row < rows; ++row)
				RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(Spacing) });

			ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Spacing) });
			for (var column = 0; column < columns.Count; ++column)
			{
				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)widths[column] / precision) });
				for (var row = 0; row < rows; ++row)
					AddChild(columns[column].Item2[row], row + 1, column + 1);
			}
			ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Spacing) });

			AddChild(new Border { CornerRadius = new CornerRadius(2), BorderThickness = new Thickness(Spacing), BorderBrush = LineBrush }, 0, 0, RowDefinitions.Count, ColumnDefinitions.Count);

			GetErrorControls().ForEach(control => AddChild(control, 0, 0, RowDefinitions.Count, ColumnDefinitions.Count));
		}
	}
}
