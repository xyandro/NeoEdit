using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes.Dialogs
{
	internal partial class GotoDialog
	{
		[DepProp]
		public long Value { get { return UIHelper<GotoDialog>.GetPropValue(() => this.Value); } set { UIHelper<GotoDialog>.SetPropValue(() => this.Value, value); } }

		static GotoDialog()
		{
			UIHelper<GotoDialog>.Register();
		}

		GotoDialog(long _Value)
		{
			InitializeComponent();

			Value = _Value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static long? Run(long value = 0)
		{
			var dialog = new GotoDialog(value);
			return dialog.ShowDialog() == true ? dialog.Value : (long?)null;
		}
	}
}
