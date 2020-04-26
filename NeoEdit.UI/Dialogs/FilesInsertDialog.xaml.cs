using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesInsertDialog
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<FilesInsertDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<FilesInsertDialog>.SetPropValue(this, value); } }

		static FilesInsertDialog() { UIHelper<FilesInsertDialog>.Register(); }

		FilesInsertDialog()
		{
			InitializeComponent();

			CodePage = Coder.CodePage.AutoByBOM;

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";
		}

		Configuration_Files_Insert result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Insert { CodePage = CodePage };
			DialogResult = true;
		}

		public static Configuration_Files_Insert Run(Window parent)
		{
			var dialog = new FilesInsertDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
