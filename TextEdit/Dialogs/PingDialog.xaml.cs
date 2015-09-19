using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class PingDialog
	{
		internal class Result
		{
			public int Timeout { get; set; }
		}

		[DepProp]
		public int Timeout { get { return UIHelper<PingDialog>.GetPropValue<int>(this); } set { UIHelper<PingDialog>.SetPropValue(this, value); } }

		static PingDialog() { UIHelper<PingDialog>.Register(); }

		PingDialog()
		{
			InitializeComponent();

			Timeout = 1000;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Timeout = Timeout };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new PingDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
