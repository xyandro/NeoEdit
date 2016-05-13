using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RotateDialog
	{
		internal class Result
		{
			public string Count { get; set; }
		}

		[DepProp]
		public string Count { get { return UIHelper<RotateDialog>.GetPropValue<string>(this); } set { UIHelper<RotateDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static RotateDialog() { UIHelper<RotateDialog>.Register(); }

		RotateDialog(NEVariables variables)
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
			var dialog = new RotateDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
