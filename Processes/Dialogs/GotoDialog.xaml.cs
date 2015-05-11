using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes.Dialogs
{
	internal partial class GotoDialog
	{
		[DepProp]
		public long Value { get { return UIHelper<GotoDialog>.GetPropValue<long>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }

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

		public static long? Run(Window parent, long value = 0)
		{
			var dialog = new GotoDialog(value) { Owner = parent };
			return dialog.ShowDialog() ? dialog.Value : (long?)null;
		}
	}
}
