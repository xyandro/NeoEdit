using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk.Dialogs
{
	public partial class Rename : Window
	{
		[DepProp]
		public string ItemName { get { return uiHelper.GetPropValue<string>(); } private set { uiHelper.SetPropValue(value); } }

		static Rename() { UIHelper<Rename>.Register(); }

		readonly UIHelper<Rename> uiHelper;
		Rename(DiskItem item)
		{
			uiHelper = new UIHelper<Rename>(this);
			InitializeComponent();

			ItemName = item.Name;

			name.Focus();
			name.CaretIndex = item.NameWoExtension.Length;
			name.Select(0, item.NameWoExtension.Length);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static string Run(DiskItem item)
		{
			var rename = new Rename(item);
			if (rename.ShowDialog() == true)
				return rename.ItemName;
			return null;
		}
	}
}
