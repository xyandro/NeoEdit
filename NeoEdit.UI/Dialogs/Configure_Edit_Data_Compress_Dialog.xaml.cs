using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Data_Compress_Dialog
	{
		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<Configure_Edit_Data_Compress_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Compress_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Compressor.Type CompressorType { get { return UIHelper<Configure_Edit_Data_Compress_Dialog>.GetPropValue<Compressor.Type>(this); } set { UIHelper<Configure_Edit_Data_Compress_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<Configure_Edit_Data_Compress_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Compress_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Data_Compress_Dialog() { UIHelper<Configure_Edit_Data_Compress_Dialog>.Register(); }

		Configure_Edit_Data_Compress_Dialog(Coder.CodePage _codePage, bool compress)
		{
			InitializeComponent();

			Title = compress ? "Compress Data" : "Decompress Data";

			inputCodePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			inputCodePage.SelectedValuePath = "Key";
			inputCodePage.DisplayMemberPath = "Value";

			compressorType.ItemsSource = Enum.GetValues(typeof(Compressor.Type)).Cast<Compressor.Type>().Where(type => type != Compressor.Type.None).ToList();

			outputCodePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			outputCodePage.SelectedValuePath = "Key";
			outputCodePage.DisplayMemberPath = "Value";

			if (compress)
			{
				InputCodePage = _codePage;
				CompressorType = Compressor.Type.GZip;
				OutputCodePage = Coder.CodePage.Hex;
			}
			else
			{
				InputCodePage = Coder.CodePage.Hex;
				CompressorType = Compressor.Type.GZip;
				OutputCodePage = _codePage;
			}
		}

		Configuration_Edit_Data_Compress result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Data_Compress { InputCodePage = InputCodePage, CompressorType = CompressorType, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Configuration_Edit_Data_Compress Run(Window parent, Coder.CodePage codePage, bool compress)
		{
			var dialog = new Configure_Edit_Data_Compress_Dialog(codePage, compress) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
