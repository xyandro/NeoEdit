using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip.Dialogs
{
	partial class AddCDDialog
	{
		[DepProp]
		public CDDrive Drive { get { return UIHelper<AddCDDialog>.GetPropValue<CDDrive>(this); } set { UIHelper<AddCDDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<CDDrive> Drives { get { return UIHelper<AddCDDialog>.GetPropValue<List<CDDrive>>(this); } set { UIHelper<AddCDDialog>.SetPropValue(this, value); } }

		static AddCDDialog() { UIHelper<AddCDDialog>.Register(); }

		AddCDDialog()
		{
			InitializeComponent();
			Drives = CDDrive.GetDrives();
		}

		CDDrive result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = Drive;
			DialogResult = true;
		}

		static public CDDrive Run(Window parent)
		{
			var dialog = new AddCDDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
