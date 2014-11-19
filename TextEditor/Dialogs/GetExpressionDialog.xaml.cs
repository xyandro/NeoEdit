using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	partial class GetExpressionDialog
	{
		internal class Result : DialogResult
		{
			public string ExpressionStr { get; set; }
			public List<string> ExpressionVars { get; set; }
			public NeoEdit.Common.Expression Expression { get; set; }
			public bool IncludeMatches { get; set; }

			public Result(string expressionStr, List<string> expressionVars, bool includeMatches)
			{
				ExpressionStr = expressionStr;
				ExpressionVars = expressionVars;
				IncludeMatches = includeMatches;
				Expression = new Common.Expression(ExpressionStr, ExpressionVars);
			}

			public override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Element(a => a.ExpressionStr),
					neXml.Element(a => a.ExpressionVars),
					neXml.Attribute(a => a.IncludeMatches)
				);
			}

			public static Result FromXML(XElement xml)
			{
				return new Result(NEXML<Result>.Element(xml, a => a.ExpressionStr), NEXML<Result>.Element(xml, a => a.ExpressionVars), NEXML<Result>.Attribute(xml, a => a.IncludeMatches));
			}
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

		readonly ExpressionData expressionData;
		GetExpressionDialog(ExpressionData _expressionData, bool getIncludeMatches)
		{
			InitializeComponent();

			expressionData = _expressionData;

			Expression = "x";
			var index = expressionData.vars.IndexOf(Expression);
			Example1 = expressionData.values.Count > 0 ? expressionData.values[0][index] : null;
			Example2 = expressionData.values.Count > 1 ? expressionData.values[1][index] : null;
			Example3 = expressionData.values.Count > 2 ? expressionData.values[2][index] : null;
			Example4 = expressionData.values.Count > 3 ? expressionData.values[3][index] : null;
			Example5 = expressionData.values.Count > 4 ? expressionData.values[4][index] : null;
			Example6 = expressionData.values.Count > 5 ? expressionData.values[5][index] : null;
			Example7 = expressionData.values.Count > 6 ? expressionData.values[6][index] : null;
			Example8 = expressionData.values.Count > 7 ? expressionData.values[7][index] : null;
			Example9 = expressionData.values.Count > 8 ? expressionData.values[8][index] : null;
			Example10 = expressionData.values.Count > 9 ? expressionData.values[9][index] : null;

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
				var expression = new NeoEdit.Common.Expression(Expression, expressionData.vars);
				if (expressionData.values.Count > 0) Example1Value = expression.Evaluate(expressionData.values[0]);
				if (expressionData.values.Count > 1) Example2Value = expression.Evaluate(expressionData.values[1]);
				if (expressionData.values.Count > 2) Example3Value = expression.Evaluate(expressionData.values[2]);
				if (expressionData.values.Count > 3) Example4Value = expression.Evaluate(expressionData.values[3]);
				if (expressionData.values.Count > 4) Example5Value = expression.Evaluate(expressionData.values[4]);
				if (expressionData.values.Count > 5) Example6Value = expression.Evaluate(expressionData.values[5]);
				if (expressionData.values.Count > 6) Example7Value = expression.Evaluate(expressionData.values[6]);
				if (expressionData.values.Count > 7) Example8Value = expression.Evaluate(expressionData.values[7]);
				if (expressionData.values.Count > 8) Example9Value = expression.Evaluate(expressionData.values[8]);
				if (expressionData.values.Count > 9) Example10Value = expression.Evaluate(expressionData.values[9]);
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
			result = new Result(Expression, expressionData.vars, IncludeMatches);
			DialogResult = true;
		}

		static internal Result Run(ExpressionData examples, bool getIncludeMatches)
		{
			var dialog = new GetExpressionDialog(examples, getIncludeMatches);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
