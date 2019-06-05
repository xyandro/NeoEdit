using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Dialogs
{
	partial class FilesSetSizeDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public long Factor { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<FilesSetSizeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSetSizeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long Factor { get { return UIHelper<FilesSetSizeDialog>.GetPropValue<long>(this); } set { UIHelper<FilesSetSizeDialog>.SetPropValue(this, value); } }
		public Dictionary<string, long> FactorDict { get; }
		public NEVariables Variables { get; }

		static FilesSetSizeDialog() { UIHelper<FilesSetSizeDialog>.Register(); }

		FilesSetSizeDialog(NEVariables variables)
		{
			FactorDict = new Dictionary<string, long>
			{
				["GB"] = 1 << 30,
				["MB"] = 1 << 20,
				["KB"] = 1 << 10,
				["bytes"] = 1 << 0,
			};
			Variables = variables;
			InitializeComponent();
			Expression = "size";
			Factor = 1;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression, Factor = Factor };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesSetSizeDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
