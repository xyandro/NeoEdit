using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class GetExpressionDialog
	{
		public class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example1 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example1Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example2 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example2Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example3 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example3Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example4 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example4Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example5 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example5Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example6 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example6Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example7 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example7Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example8 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example8Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example9 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example9Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Example10 { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Example10Value { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }

		static GetExpressionDialog()
		{
			UIHelper<GetExpressionDialog>.Register();
			UIHelper<GetExpressionDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.EvaluateExamples());
		}

		readonly Dictionary<string, List<object>> expressionData;
		readonly Action helpDialog;
		GetExpressionDialog(Dictionary<string, List<object>> expressionData, Action helpDialog)
		{
			this.expressionData = expressionData;
			this.helpDialog = helpDialog;

			InitializeComponent();

			var list = expressionData["x"];
			Example1 = (list.Count > 0) && (list[0] != null) ? list[0].ToString() : null;
			Example2 = (list.Count > 1) && (list[1] != null) ? list[1].ToString() : null;
			Example3 = (list.Count > 2) && (list[2] != null) ? list[2].ToString() : null;
			Example4 = (list.Count > 3) && (list[3] != null) ? list[3].ToString() : null;
			Example5 = (list.Count > 4) && (list[4] != null) ? list[4].ToString() : null;
			Example6 = (list.Count > 5) && (list[5] != null) ? list[5].ToString() : null;
			Example7 = (list.Count > 6) && (list[6] != null) ? list[6].ToString() : null;
			Example8 = (list.Count > 7) && (list[7] != null) ? list[7].ToString() : null;
			Example9 = (list.Count > 8) && (list[8] != null) ? list[8].ToString() : null;
			Example10 = (list.Count > 9) && (list[9] != null) ? list[9].ToString() : null;

			Expression = "x";
			expression.CaretIndex = 1;
		}

		void EvaluateExamples()
		{
			bool valid = true;
			try
			{
				var expression = new NEExpression(Expression);
				if (Example1 != null) Example1Value = expression.EvaluateRow(expressionData, 0);
				if (Example2 != null) Example2Value = expression.EvaluateRow(expressionData, 1);
				if (Example3 != null) Example3Value = expression.EvaluateRow(expressionData, 2);
				if (Example4 != null) Example4Value = expression.EvaluateRow(expressionData, 3);
				if (Example5 != null) Example5Value = expression.EvaluateRow(expressionData, 4);
				if (Example6 != null) Example6Value = expression.EvaluateRow(expressionData, 5);
				if (Example7 != null) Example7Value = expression.EvaluateRow(expressionData, 6);
				if (Example8 != null) Example8Value = expression.EvaluateRow(expressionData, 7);
				if (Example9 != null) Example9Value = expression.EvaluateRow(expressionData, 8);
				if (Example10 != null) Example10Value = expression.EvaluateRow(expressionData, 9);
				valid = true;
			}
			catch
			{
				Example1Value = Example2Value = Example3Value = Example4Value = Example5Value = Example6Value = Example7Value = Example8Value = Example9Value = Example10Value = null;
				valid = false;
			}

			example1Value.SetValidation(TextBox.TextProperty, valid);
			example2Value.SetValidation(TextBox.TextProperty, valid);
			example3Value.SetValidation(TextBox.TextProperty, valid);
			example4Value.SetValidation(TextBox.TextProperty, valid);
			example5Value.SetValidation(TextBox.TextProperty, valid);
			example6Value.SetValidation(TextBox.TextProperty, valid);
			example7Value.SetValidation(TextBox.TextProperty, valid);
			example8Value.SetValidation(TextBox.TextProperty, valid);
			example9Value.SetValidation(TextBox.TextProperty, valid);
			example10Value.SetValidation(TextBox.TextProperty, valid);
		}

		void ExpressionHelp(object sender, RoutedEventArgs e)
		{
			if (helpDialog != null)
				helpDialog();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, Dictionary<string, List<object>> examples, Action helpDialog = null)
		{
			var dialog = new GetExpressionDialog(examples, helpDialog) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
