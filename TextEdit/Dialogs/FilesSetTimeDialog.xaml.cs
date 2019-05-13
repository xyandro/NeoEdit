using System.Windows;
using NeoEdit.TextEdit.Expressions;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesSetTimeDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public NEVariables Variables { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<FilesSetTimeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSetTimeDialog>.SetPropValue(this, value); } }

		static FilesSetTimeDialog() { UIHelper<FilesSetTimeDialog>.Register(); }

		FilesSetTimeDialog(NEVariables variables, string expression)
		{
			InitializeComponent();

			Variables = variables;
			Expression = expression;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables, string expression)
		{
			var dialog = new FilesSetTimeDialog(variables, expression) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
