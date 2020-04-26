using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Operations_Encoding_Dialog
	{
		[DepProp]
		public Coder.CodePage InputCodePage { get { return UIHelper<Configure_Files_Operations_Encoding_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Files_Operations_Encoding_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputCodePage { get { return UIHelper<Configure_Files_Operations_Encoding_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Files_Operations_Encoding_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Operations_Encoding_Dialog()
		{
			UIHelper<Configure_Files_Operations_Encoding_Dialog>.Register();
		}

		Configure_Files_Operations_Encoding_Dialog()
		{
			InitializeComponent();
			inputCodePage.ItemsSource = outputCodePage.ItemsSource = Coder.GetAllCodePages().Select(codePage => Tuple.Create(codePage, Coder.GetDescription(codePage)));
			inputCodePage.SelectedValuePath = outputCodePage.SelectedValuePath = "Item1";
			inputCodePage.DisplayMemberPath = outputCodePage.DisplayMemberPath = "Item2";
			InputCodePage = Coder.CodePage.AutoByBOM;
			OutputCodePage = Coder.CodePage.UTF8;
		}

		Configuration_Files_Operations_Encoding result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Operations_Encoding { InputCodePage = InputCodePage, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Configuration_Files_Operations_Encoding Run(Window parent)
		{
			var dialog = new Configure_Files_Operations_Encoding_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
