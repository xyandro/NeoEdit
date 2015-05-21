using NeoEdit.GUI.Controls;

namespace NeoEdit.TextView.Dialogs
{
	partial class MultiProgressDialogProgress
	{
		[DepProp]
		public string ItemName { get { return UIHelper<MultiProgressDialogProgress>.GetPropValue<string>(this); } set { UIHelper<MultiProgressDialogProgress>.SetPropValue(this, value); } }
		[DepProp]
		public int Progress { get { return UIHelper<MultiProgressDialogProgress>.GetPropValue<int>(this); } set { UIHelper<MultiProgressDialogProgress>.SetPropValue(this, value); } }

		static MultiProgressDialogProgress() { UIHelper<MultiProgressDialogProgress>.Register(); }

		public MultiProgressDialogProgress(string name)
		{
			InitializeComponent();
			ItemName = name;
		}
	}
}
