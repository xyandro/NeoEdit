using System;
using System.IO;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Disk.Dialogs
{
	partial class RenameDialog
	{
		[DepProp]
		public string ItemName { get { return UIHelper<RenameDialog>.GetPropValue<string>(this); } private set { UIHelper<RenameDialog>.SetPropValue(this, value); } }
		public string FullName { get { return item.Path + @"\" + ItemName; } }

		static RenameDialog() { UIHelper<RenameDialog>.Register(); }

		readonly DiskItem item;
		RenameDialog(DiskItem item)
		{
			InitializeComponent();

			label.Content = String.Format("Please enter new name for {0}:", item.Name);
			this.item = item;
			ItemName = item.Name;

			name.Focus();
			name.CaretIndex = item.NameWoExtension.Length;
			name.Select(0, name.CaretIndex);
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

		public static string Run(Window parent, DiskItem item)
		{
			var dialog = new RenameDialog(item) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;
			return dialog.FullName;
		}
	}
}
