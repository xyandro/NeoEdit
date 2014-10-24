using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	class DiskMenuItem : NEMenuItem<DiskCommand> { }

	partial class DiskMenu
	{
		[DepProp]
		public new DiskTabs Parent { get { return UIHelper<DiskMenu>.GetPropValue<DiskTabs>(this); } set { UIHelper<DiskMenu>.SetPropValue(this, value); } }

		static DiskMenu() { UIHelper<DiskMenu>.Register(); }

		public DiskMenu()
		{
			InitializeComponent();
		}
	}
}
