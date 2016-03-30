using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectRotateDialog
	{
		internal class Result
		{
			public string Count { get; set; }
		}

		[DepProp]
		public string Count { get { return UIHelper<SelectRotateDialog>.GetPropValue<string>(this); } set { UIHelper<SelectRotateDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static SelectRotateDialog() { UIHelper<SelectRotateDialog>.Register(); }

		SelectRotateDialog(NEVariables variables)
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
			var dialog = new SelectRotateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
