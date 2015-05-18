using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FindElementByNameDialog
	{
		internal class Result
		{
			public string NameToFind { get; set; }
		}

		[DepProp]
		public string NameToFind { get { return UIHelper<FindElementByNameDialog>.GetPropValue<string>(this); } set { UIHelper<FindElementByNameDialog>.SetPropValue(this, value); } }

		static FindElementByNameDialog()
		{
			UIHelper<FindElementByNameDialog>.Register();
		}

		FindElementByNameDialog()
		{
			InitializeComponent();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { NameToFind = NameToFind };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FindElementByNameDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
