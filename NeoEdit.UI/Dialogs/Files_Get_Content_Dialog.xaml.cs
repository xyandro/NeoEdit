using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Get_Content_Dialog
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<Files_Get_Content_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Files_Get_Content_Dialog>.SetPropValue(this, value); } }

		static Files_Get_Content_Dialog() { UIHelper<Files_Get_Content_Dialog>.Register(); }

		Files_Get_Content_Dialog()
		{
			InitializeComponent();

			CodePage = Coder.CodePage.AutoByBOM;

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";
		}

		Configuration_Files_Get_Content result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Get_Content { CodePage = CodePage };
			DialogResult = true;
		}

		public static Configuration_Files_Get_Content Run(Window parent)
		{
			var dialog = new Files_Get_Content_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
