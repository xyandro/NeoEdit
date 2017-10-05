using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FilesFindMassFindDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public bool MatchCase { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }

		static bool matchCaseVal;

		static FilesFindMassFindDialog() { UIHelper<FilesFindMassFindDialog>.Register(); }

		FilesFindMassFindDialog(NEVariables variables)
		{
			InitializeComponent();

			Expression = expression.GetLastSuggestion().CoalesceNullOrEmpty("k");
			MatchCase = matchCaseVal;
			Variables = variables;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Expression))
				return;

			result = new Result { Expression = Expression, MatchCase = MatchCase };

			matchCaseVal = MatchCase;

			expression.AddCurrentSuggestion();

			DialogResult = true;
		}

		void Reset(object sender, RoutedEventArgs e) => MatchCase = false;

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesFindMassFindDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
