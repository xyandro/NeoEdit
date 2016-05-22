using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class CopyMoveFilesDialog
	{
		internal class Result
		{
			public string OldFileName { get; set; }
			public string NewFileName { get; set; }
		}

		[DepProp]
		public string OldFileName { get { return UIHelper<CopyMoveFilesDialog>.GetPropValue<string>(this); } set { UIHelper<CopyMoveFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewFileName { get { return UIHelper<CopyMoveFilesDialog>.GetPropValue<string>(this); } set { UIHelper<CopyMoveFilesDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static CopyMoveFilesDialog() { UIHelper<CopyMoveFilesDialog>.Register(); }

		CopyMoveFilesDialog(NEVariables variables, bool move)
		{
			Variables = variables;
			InitializeComponent();

			Title = move ? "Move Files" : "Copy Files";

			OldFileName = NewFileName = "x";
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { OldFileName = OldFileName, NewFileName = NewFileName };
			oldFileName.AddCurrentSuggestion();
			newFileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables, bool move)
		{
			var dialog = new CopyMoveFilesDialog(variables, move) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
