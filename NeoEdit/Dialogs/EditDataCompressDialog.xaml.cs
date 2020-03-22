using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class EditDataCompressDialog
	{
		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<EditDataCompressDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataCompressDialog>.SetPropValue(this, value); } }
		[DepProp]
		Compressor.Type CompressorType { get { return UIHelper<EditDataCompressDialog>.GetPropValue<Compressor.Type>(this); } set { UIHelper<EditDataCompressDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<EditDataCompressDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataCompressDialog>.SetPropValue(this, value); } }

		static EditDataCompressDialog() { UIHelper<EditDataCompressDialog>.Register(); }

		EditDataCompressDialog(Coder.CodePage _codePage, bool compress)
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

		EditDataCompressDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EditDataCompressDialogResult { InputCodePage = InputCodePage, CompressorType = CompressorType, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static EditDataCompressDialogResult Run(Window parent, Coder.CodePage codePage, bool compress)
		{
			var dialog = new EditDataCompressDialog(codePage, compress) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
