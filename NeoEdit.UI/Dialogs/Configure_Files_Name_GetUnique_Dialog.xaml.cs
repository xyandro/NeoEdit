using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Name_GetUnique_Dialog
	{
		[DepProp]
		public string Format { get { return UIHelper<Configure_Files_Name_GetUnique_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Name_GetUnique_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckExisting { get { return UIHelper<Configure_Files_Name_GetUnique_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Name_GetUnique_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RenameAll { get { return UIHelper<Configure_Files_Name_GetUnique_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Name_GetUnique_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool UseGUIDs { get { return UIHelper<Configure_Files_Name_GetUnique_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Name_GetUnique_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Name_GetUnique_Dialog() { UIHelper<Configure_Files_Name_GetUnique_Dialog>.Register(); }

		Configure_Files_Name_GetUnique_Dialog()
		{
			InitializeComponent();
			FormatClick(keepName, null);
			CheckExisting = true;
		}

		Configuration_Files_Name_GetUnique result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Name_GetUnique { Format = Format, CheckExisting = CheckExisting, RenameAll = RenameAll, UseGUIDs = UseGUIDs };
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

		public static Configuration_Files_Name_GetUnique Run(Window parent)
		{
			var dialog = new Configure_Files_Name_GetUnique_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
