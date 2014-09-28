using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class ExpressionDialog : Window
	{
		[DepProp]
		public string Expression { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IsExpression { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example1 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example1Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example2 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example2Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example3 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example3Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example4 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example4Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example5 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example5Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example6 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example6Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example7 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example7Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example8 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example8Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example9 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example9Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Example10 { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public object Example10Value { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static ExpressionDialog() { UIHelper<ExpressionDialog>.Register(); }

		readonly UIHelper<ExpressionDialog> uiHelper;
		ExpressionDialog(List<string> examples, bool isExpression)
		{
			uiHelper = new UIHelper<ExpressionDialog>(this);
			InitializeComponent();

			uiHelper.AddCallback(a => a.Expression, (o, n) => EvaluateExamples());

			Example1 = examples.Count >= 1 ? examples[0] : null;
			Example2 = examples.Count >= 2 ? examples[1] : null;
			Example3 = examples.Count >= 3 ? examples[2] : null;
			Example4 = examples.Count >= 4 ? examples[3] : null;
			Example5 = examples.Count >= 5 ? examples[4] : null;
			Example6 = examples.Count >= 6 ? examples[5] : null;
			Example7 = examples.Count >= 7 ? examples[6] : null;
			Example8 = examples.Count >= 8 ? examples[7] : null;
			Example9 = examples.Count >= 9 ? examples[8] : null;
			Example10 = examples.Count >= 10 ? examples[9] : null;

			IsExpression = isExpression;
			Expression = isExpression ? "x" : "^$";
			expression.CaretIndex = 1;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		void EvaluateExamples()
		{
			bool valid = true;
			try
			{
				if (IsExpression)
				{
					var expression = new NeoEdit.Common.Expression(Expression);
					Example1Value = expression.Evaluate(Example1, 1);
					Example2Value = expression.Evaluate(Example2, 2);
					Example3Value = expression.Evaluate(Example3, 3);
					Example4Value = expression.Evaluate(Example4, 4);
					Example5Value = expression.Evaluate(Example5, 5);
					Example6Value = expression.Evaluate(Example6, 6);
					Example7Value = expression.Evaluate(Example7, 7);
					Example8Value = expression.Evaluate(Example8, 8);
					Example9Value = expression.Evaluate(Example9, 9);
					Example10Value = expression.Evaluate(Example10, 10);
				}
				else
				{
					var expression = new Regex(Expression);
					Example1Value = expression.IsMatch(Example1);
					Example2Value = expression.IsMatch(Example2);
					Example3Value = expression.IsMatch(Example3);
					Example4Value = expression.IsMatch(Example4);
					Example5Value = expression.IsMatch(Example5);
					Example6Value = expression.IsMatch(Example6);
					Example7Value = expression.IsMatch(Example7);
					Example8Value = expression.IsMatch(Example8);
					Example9Value = expression.IsMatch(Example9);
					Example10Value = expression.IsMatch(Example10);
				}
				valid = true;
			}
			catch
			{
				Example1Value = Example2Value = Example3Value = Example4Value = Example5Value = Example6Value = Example7Value = Example8Value = Example9Value = Example10Value = null;
				valid = false;
			}

			uiHelper.SetValidation(example1Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example2Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example3Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example4Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example5Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example6Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example7Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example8Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example9Value, TextBox.TextProperty, valid);
			uiHelper.SetValidation(example10Value, TextBox.TextProperty, valid);
		}

		static public NeoEdit.Common.Expression GetExpression(List<string> examples)
		{
			var dialog = new ExpressionDialog(examples, true);
			if (dialog.ShowDialog() != true)
				return null;

			return new NeoEdit.Common.Expression(dialog.Expression);
		}

		static public Regex GetRegEx(List<string> examples)
		{
			var dialog = new ExpressionDialog(examples, false);
			if (dialog.ShowDialog() != true)
				return null;

			return new Regex(dialog.Expression);
		}
	}
}
