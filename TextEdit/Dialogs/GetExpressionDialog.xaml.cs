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
		}

		[DepProp]
		public string Expression { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Expression); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Expression, value); } }
		[DepProp]
		public string Example1 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example1); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example1, value); } }
		[DepProp]
		public string Example1Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example1Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example1Value, value); } }
		[DepProp]
		public string Example2 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example2); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example2, value); } }
		[DepProp]
		public string Example2Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example2Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example2Value, value); } }
		[DepProp]
		public string Example3 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example3); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example3, value); } }
		[DepProp]
		public string Example3Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example3Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example3Value, value); } }
		[DepProp]
		public string Example4 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example4); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example4, value); } }
		[DepProp]
		public string Example4Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example4Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example4Value, value); } }
		[DepProp]
		public string Example5 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example5); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example5, value); } }
		[DepProp]
		public string Example5Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example5Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example5Value, value); } }
		[DepProp]
		public string Example6 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example6); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example6, value); } }
		[DepProp]
		public string Example6Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example6Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example6Value, value); } }
		[DepProp]
		public string Example7 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example7); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example7, value); } }
		[DepProp]
		public string Example7Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example7Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example7Value, value); } }
		[DepProp]
		public string Example8 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example8); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example8, value); } }
		[DepProp]
		public string Example8Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example8Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example8Value, value); } }
		[DepProp]
		public string Example9 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example9); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example9, value); } }
		[DepProp]
		public string Example9Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example9Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example9Value, value); } }
		[DepProp]
		public string Example10 { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example10); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example10, value); } }
		[DepProp]
		public string Example10Value { get { return UIHelper<GetExpressionDialog>.GetPropValue(() => this.Example10Value); } set { UIHelper<GetExpressionDialog>.SetPropValue(() => this.Example10Value, value); } }

		static GetExpressionDialog()
		{
			UIHelper<GetExpressionDialog>.Register();
			UIHelper<GetExpressionDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.EvaluateExamples());
		}

		readonly Dictionary<string, List<string>> expressionData;
		GetExpressionDialog(Dictionary<string, List<string>> expressionData)
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
		}

		void EvaluateExamples()
		{
			bool valid = true;
			try
			{
				var expression = new NeoEdit.Common.Expression(Expression, expressionData.Keys);
				if (Example1 != null) Example1Value = expression.EvaluateDict(expressionData, 0).ToString();
				if (Example2 != null) Example2Value = expression.EvaluateDict(expressionData, 1).ToString();
				if (Example3 != null) Example3Value = expression.EvaluateDict(expressionData, 2).ToString();
				if (Example4 != null) Example4Value = expression.EvaluateDict(expressionData, 3).ToString();
				if (Example5 != null) Example5Value = expression.EvaluateDict(expressionData, 4).ToString();
				if (Example6 != null) Example6Value = expression.EvaluateDict(expressionData, 5).ToString();
				if (Example7 != null) Example7Value = expression.EvaluateDict(expressionData, 6).ToString();
				if (Example8 != null) Example8Value = expression.EvaluateDict(expressionData, 7).ToString();
				if (Example9 != null) Example9Value = expression.EvaluateDict(expressionData, 8).ToString();
				if (Example10 != null) Example10Value = expression.EvaluateDict(expressionData, 9).ToString();
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = new Common.Expression(Expression, expressionData.Keys) };
			DialogResult = true;
		}

		static internal Result Run(Dictionary<string, List<string>> examples)
		{
			var dialog = new GetExpressionDialog(examples);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
