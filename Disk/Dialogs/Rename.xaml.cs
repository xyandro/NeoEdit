using System;
using System.IO;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Disk.Dialogs
{
	public partial class Rename : Window
	{
		[DepProp]
		public string ItemName { get { return UIHelper<Rename>.GetPropValue<string>(this); } private set { UIHelper<Rename>.SetPropValue(this, value); } }
		public string FullName { get { return path + @"\" + ItemName; } }

		static Rename() { UIHelper<Rename>.Register(); }

		readonly string path;
		Rename(DiskItem item)
		{
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
			if (rename.ShowDialog() != true)
				return null;
			return rename.FullName;
		}
	}
}
