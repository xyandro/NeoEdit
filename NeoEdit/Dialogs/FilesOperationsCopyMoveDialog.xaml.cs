using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Expressions;

namespace NeoEdit.Dialogs
{
	partial class FilesOperationsCopyMoveDialog
	{
		public class Result
		{
			public string OldFileName { get; set; }
			public string NewFileName { get; set; }
		}

		[DepProp]
		public string OldFileName { get { return UIHelper<FilesOperationsCopyMoveDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCopyMoveDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewFileName { get { return UIHelper<FilesOperationsCopyMoveDialog>.GetPropValue<string>(this); } set { UIHelper<FilesOperationsCopyMoveDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static FilesOperationsCopyMoveDialog() { UIHelper<FilesOperationsCopyMoveDialog>.Register(); }

		FilesOperationsCopyMoveDialog(NEVariables variables, bool move)
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
			var dialog = new FilesOperationsCopyMoveDialog(variables, move) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
