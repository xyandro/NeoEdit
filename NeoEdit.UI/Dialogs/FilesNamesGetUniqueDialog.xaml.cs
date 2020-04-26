using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesNamesGetUniqueDialog
	{
		[DepProp]
		public string Format { get { return UIHelper<FilesNamesGetUniqueDialog>.GetPropValue<string>(this); } set { UIHelper<FilesNamesGetUniqueDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckExisting { get { return UIHelper<FilesNamesGetUniqueDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesNamesGetUniqueDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RenameAll { get { return UIHelper<FilesNamesGetUniqueDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesNamesGetUniqueDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool UseGUIDs { get { return UIHelper<FilesNamesGetUniqueDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesNamesGetUniqueDialog>.SetPropValue(this, value); } }

		static FilesNamesGetUniqueDialog() { UIHelper<FilesNamesGetUniqueDialog>.Register(); }

		FilesNamesGetUniqueDialog()
		{
			InitializeComponent();
			FormatClick(keepName, null);
			CheckExisting = true;
		}

		FilesNamesGetUniqueDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesNamesGetUniqueDialogResult { Format = Format, CheckExisting = CheckExisting, RenameAll = RenameAll, UseGUIDs = UseGUIDs };
			DialogResult = true;
		}

		void FormatClick(object sender, RoutedEventArgs e)
		{
			if (sender == keepName)
			{
				Format = @"{Path}{Name} ({Unique}){Ext}";
				RenameAll = false;
			}
			else
			{
				Format = @"{Path}{Unique}{Ext}";
				RenameAll = true;
			}
		}

		public static FilesNamesGetUniqueDialogResult Run(Window parent)
		{
			var dialog = new FilesNamesGetUniqueDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
