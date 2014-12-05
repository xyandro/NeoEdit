using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
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

		readonly Dictionary<string, List<string>> expressionData;
		GetExpressionDialog(Dictionary<string, List<string>> expressionData, bool getIncludeMatches)
		{
			InitializeComponent();

			this.expressionData = expressionData;

			Expression = "x";
			var list = expressionData[Expression];
			Example1 = list.Count > 0 ? list[0] : null;
			Example2 = list.Count > 1 ? list[1] : null;
			Example3 = list.Count > 2 ? list[2] : null;
			Example4 = list.Count > 3 ? list[3] : null;
			Example5 = list.Count > 4 ? list[4] : null;
			Example6 = list.Count > 5 ? list[5] : null;
			Example7 = list.Count > 6 ? list[6] : null;
			Example8 = list.Count > 7 ? list[7] : null;
			Example9 = list.Count > 8 ? list[8] : null;
			Example10 = list.Count > 9 ? list[9] : null;

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
				var expression = new NeoEdit.Common.Expression(Expression, expressionData.Keys);
				if (Example1 != null) Example1Value = expression.EvaluateDict(expressionData, 0);
				if (Example2 != null) Example2Value = expression.EvaluateDict(expressionData, 1);
				if (Example3 != null) Example3Value = expression.EvaluateDict(expressionData, 2);
				if (Example4 != null) Example4Value = expression.EvaluateDict(expressionData, 3);
				if (Example5 != null) Example5Value = expression.EvaluateDict(expressionData, 4);
				if (Example6 != null) Example6Value = expression.EvaluateDict(expressionData, 5);
				if (Example7 != null) Example7Value = expression.EvaluateDict(expressionData, 6);
				if (Example8 != null) Example8Value = expression.EvaluateDict(expressionData, 7);
				if (Example9 != null) Example9Value = expression.EvaluateDict(expressionData, 8);
				if (Example10 != null) Example10Value = expression.EvaluateDict(expressionData, 9);
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
			result = new Result { Expression = new Common.Expression(Expression, expressionData.Keys), IncludeMatches = IncludeMatches };
			DialogResult = true;
		}

		static internal Result Run(Dictionary<string, List<string>> examples, bool getIncludeMatches)
		{
			var dialog = new GetExpressionDialog(examples, getIncludeMatches);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
