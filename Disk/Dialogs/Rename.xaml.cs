using System;
using System.IO;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Disk.Dialogs
{
	public partial class Rename : TransparentWindow
	{
		[DepProp]
		public string ItemName { get { return uiHelper.GetPropValue<string>(); } private set { uiHelper.SetPropValue(value); } }
		public string FullName { get { return path + @"\" + ItemName; } }

		static Rename() { UIHelper<Rename>.Register(); }

		readonly UIHelper<Rename> uiHelper;
		readonly string path;
		Rename(DiskItem item)
		{
			uiHelper = new UIHelper<Rename>(this);
			InitializeComponent();

			label.Content = String.Format("Please enter new name for {0}:", item.Name);

			ItemName = item.Name;
			path = item.Path;

			name.Focus();
			name.CaretIndex = item.NameWoExtension.Length;
			name.Select(0, item.NameWoExtension.Length);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((File.Exists(FullName)) || (Directory.Exists(FullName)))
			{
				Message.Show("A file or directory already exists at that location.");
				return;
			}

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
