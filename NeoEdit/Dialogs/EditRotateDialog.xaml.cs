using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Expressions;

namespace NeoEdit.Dialogs
{
	partial class EditRotateDialog
	{
		public class Result
		{
			public string Count { get; set; }
		}

		[DepProp]
		public string Count { get { return UIHelper<EditRotateDialog>.GetPropValue<string>(this); } set { UIHelper<EditRotateDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static EditRotateDialog() { UIHelper<EditRotateDialog>.Register(); }

		EditRotateDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Count = "1";
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			count.AddCurrentSuggestion();
			result = new Result { Count = Count };
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new EditRotateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
