using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesOperationsEncodingDialog
	{
		[DepProp]
		public Coder.CodePage InputCodePage { get { return UIHelper<FilesOperationsEncodingDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<FilesOperationsEncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputCodePage { get { return UIHelper<FilesOperationsEncodingDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<FilesOperationsEncodingDialog>.SetPropValue(this, value); } }

		static FilesOperationsEncodingDialog()
		{
			UIHelper<FilesOperationsEncodingDialog>.Register();
		}

		FilesOperationsEncodingDialog()
		{
			InitializeComponent();
			inputCodePage.ItemsSource = outputCodePage.ItemsSource = Coder.GetAllCodePages().Select(codePage => Tuple.Create(codePage, Coder.GetDescription(codePage)));
			inputCodePage.SelectedValuePath = outputCodePage.SelectedValuePath = "Item1";
			inputCodePage.DisplayMemberPath = outputCodePage.DisplayMemberPath = "Item2";
			InputCodePage = Coder.CodePage.AutoByBOM;
			OutputCodePage = Coder.CodePage.UTF8;
		}

		FilesOperationsEncodingDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesOperationsEncodingDialogResult { InputCodePage = InputCodePage, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		static public FilesOperationsEncodingDialogResult Run(Window parent)
		{
			var dialog = new FilesOperationsEncodingDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
