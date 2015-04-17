using NeoEdit.GUI.Common;

namespace NeoEdit.TextView.Dialogs
{
	partial class MultiProgressDialogProgress
	{
		[DepProp]
		public string ItemName { get { return UIHelper<MultiProgressDialogProgress>.GetPropValue(() => this.ItemName); } set { UIHelper<MultiProgressDialogProgress>.SetPropValue(() => this.ItemName, value); } }
		[DepProp]
		public int Progress { get { return UIHelper<MultiProgressDialogProgress>.GetPropValue(() => this.Progress); } set { UIHelper<MultiProgressDialogProgress>.SetPropValue(() => this.Progress, value); } }

		static MultiProgressDialogProgress() { UIHelper<MultiProgressDialogProgress>.Register(); }

		public MultiProgressDialogProgress(string name)
		{
			InitializeComponent();
			ItemName = name;
		}
	}
}
