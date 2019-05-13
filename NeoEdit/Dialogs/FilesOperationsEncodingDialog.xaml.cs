using System;
using System.Linq;
using System.Windows;
using NeoEdit.Transform;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	internal partial class FilesOperationsEncodingDialog
	{
		internal class Result
		{
			public Coder.CodePage InputCodePage { get; set; }
			public Coder.CodePage OutputCodePage { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputCodePage = InputCodePage, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new FilesOperationsEncodingDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
