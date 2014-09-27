using System.Collections.Generic;
using System.Linq;
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
		ExpressionDialog(List<string> examples)
		{
			uiHelper = new UIHelper<ExpressionDialog>(this);
			InitializeComponent();

			uiHelper.AddCallback(a => a.Expression, (o, n) => EvaluateExamples());

			examples = examples.Distinct().ToList();
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

			Expression = "[0]";
			expression.CaretIndex = expression.Text.Length;
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
				var expression = new NeoEdit.Common.Expression(Expression);
				Example1Value = expression.Evaluate(Example1);
				Example2Value = expression.Evaluate(Example2);
				Example3Value = expression.Evaluate(Example3);
				Example4Value = expression.Evaluate(Example4);
				Example5Value = expression.Evaluate(Example5);
				Example6Value = expression.Evaluate(Example6);
				Example7Value = expression.Evaluate(Example7);
				Example8Value = expression.Evaluate(Example8);
				Example9Value = expression.Evaluate(Example9);
				Example10Value = expression.Evaluate(Example10);
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
		static public string Run(List<string> examples)
		{
			var dialog = new ExpressionDialog(examples);
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.Expression;
		}
	}
}
