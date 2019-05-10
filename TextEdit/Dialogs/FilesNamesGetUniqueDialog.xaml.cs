using System.Windows;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesNamesGetUniqueDialog
	{
		internal class Result
		{
			public string Format { get; set; }
			public bool CheckExisting { get; set; }
			public bool RenameAll { get; set; }
			public bool UseGUIDs { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Format = Format, CheckExisting = CheckExisting, RenameAll = RenameAll, UseGUIDs = UseGUIDs };
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

		public static Result Run(Window parent)
		{
			var dialog = new FilesNamesGetUniqueDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
