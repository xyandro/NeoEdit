using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class RandomDataDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
			public string Chars { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<RandomDataDialog>.GetPropValue<string>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Chars { get { return UIHelper<RandomDataDialog>.GetPropValue<string>(this); } set { UIHelper<RandomDataDialog>.SetPropValue(this, value); } }
		public Dictionary<string, List<object>> ExpressionData { get; }

		static RandomDataDialog() { UIHelper<RandomDataDialog>.Register(); }

		RandomDataDialog(Dictionary<string, List<object>> expressionData)
		{
			ExpressionData = expressionData;
			InitializeComponent();
			Chars = "a-zA-Z";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var chars = Misc.GetCharsFromRegexString(Chars);
			if (chars.Length == 0)
				return;

			result = new Result { Expression = Expression, Chars = chars };
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		public static Result Run(Dictionary<string, List<object>> expressionData, Window parent)
		{
			var dialog = new RandomDataDialog(expressionData) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
