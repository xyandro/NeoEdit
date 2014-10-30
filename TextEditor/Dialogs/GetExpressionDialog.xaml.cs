using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	partial class GetExpressionDialog
	{
		internal class Result
		{
			public NeoEdit.Common.Expression Expression { get; set; }
			public bool IncludeMatches { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<GetExpressionDialog>.GetPropValue<string>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeMatches { get { return UIHelper<GetExpressionDialog>.GetPropValue<bool>(this); } set { UIHelper<GetExpressionDialog>.SetPropValue(this, value); } }
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

		GetExpressionDialog(List<string> examples, bool getIncludeMatches)
		{
			InitializeComponent();

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

			Expression = "x";
			expression.CaretIndex = 1;
			IncludeMatches = true;
			if (!getIncludeMatches)
				includeMatches.Visibility = Visibility.Hidden;
		}

		void EvaluateExamples()
		{
			bool valid = true;
			try
			{
				var expression = new NeoEdit.Common.Expression(Expression);
				if (!String.IsNullOrEmpty(Example1)) Example1Value = expression.Evaluate(Example1, 1);
				if (!String.IsNullOrEmpty(Example2)) Example2Value = expression.Evaluate(Example2, 2);
				if (!String.IsNullOrEmpty(Example3)) Example3Value = expression.Evaluate(Example3, 3);
				if (!String.IsNullOrEmpty(Example4)) Example4Value = expression.Evaluate(Example4, 4);
				if (!String.IsNullOrEmpty(Example5)) Example5Value = expression.Evaluate(Example5, 5);
				if (!String.IsNullOrEmpty(Example6)) Example6Value = expression.Evaluate(Example6, 6);
				if (!String.IsNullOrEmpty(Example7)) Example7Value = expression.Evaluate(Example7, 7);
				if (!String.IsNullOrEmpty(Example8)) Example8Value = expression.Evaluate(Example8, 8);
				if (!String.IsNullOrEmpty(Example9)) Example9Value = expression.Evaluate(Example9, 9);
				if (!String.IsNullOrEmpty(Example10)) Example10Value = expression.Evaluate(Example10, 10);
				valid = true;
			}
			catch
			{
				Example1Value = Example2Value = Example3Value = Example4Value = Example5Value = Example6Value = Example7Value = Example8Value = Example9Value = Example10Value = null;
				valid = false;
			}

			UIHelper<GetExpressionDialog>.SetValidation(example1Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example2Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example3Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example4Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example5Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example6Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example7Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example8Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example9Value, TextBox.TextProperty, valid);
			UIHelper<GetExpressionDialog>.SetValidation(example10Value, TextBox.TextProperty, valid);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = new NeoEdit.Common.Expression(Expression), IncludeMatches = IncludeMatches };
			DialogResult = true;
		}

		static internal Result Run(List<string> examples, bool getIncludeMatches)
		{
			var dialog = new GetExpressionDialog(examples, getIncludeMatches);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
