using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class WidthDialog
	{
		public enum WidthType
		{
			Absolute,
			Relative,
			Minimum,
			Maximum,
			Multiple,
		}

		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		internal class Result
		{
			public WidthType Type { get; set; }
			public string Expression { get; set; }
			public char PadChar { get; set; }
			public TextLocation Location { get; set; }
		}

		[DepProp]
		public WidthType Type { get { return UIHelper<WidthDialog>.GetPropValue<WidthType>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextLocation Location { get { return UIHelper<WidthDialog>.GetPropValue<TextLocation>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsSelect { get { return UIHelper<WidthDialog>.GetPropValue<bool>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		public Dictionary<string, List<object>> ExpressionData { get; }

		static WidthDialog()
		{
			UIHelper<WidthDialog>.Register();
			UIHelper<WidthDialog>.AddCallback(a => a.Type, (obj, o, n) => obj.SetValueParams());
		}

		readonly int minLength, maxLength;
		WidthDialog(int minLength, int maxLength, bool numeric, bool isSelect, Dictionary<string, List<object>> expressionData)
		{
			ExpressionData = expressionData;
			InitializeComponent();

			this.minLength = minLength;
			this.maxLength = maxLength;
			padChar.GotFocus += (s, e) => padChar.SelectAll();

			IsSelect = isSelect;

			Type = WidthType.Absolute;
			Expression = maxLength.ToString();
			if (numeric)
				NumericClick(null, null);
			else
				StringClick(null, null);
		}

		void SetValueParams()
		{
			if ((String.IsNullOrWhiteSpace(Expression)) || (Expression.IsNumeric()))
				Expression = (Type == WidthType.Multiple ? 1 : Type == WidthType.Relative ? 0 : Type == WidthType.Minimum ? minLength : maxLength).ToString();
		}

		void NumericClick(object sender, RoutedEventArgs e)
		{
			PadChar = "0";
			Location = TextLocation.End;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			PadChar = " ";
			Location = TextLocation.Start;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Type = Type, Expression = Expression, PadChar = PadChar[0], Location = Location };
			DialogResult = true;
		}

		public static Result Run(Window parent, int minLength, int maxLength, bool numeric, bool isSelect, Dictionary<string, List<object>> expressionData)
		{
			var dialog = new WidthDialog(minLength, maxLength, numeric, isSelect, expressionData) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
