using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class CompressDataDialog
	{
		public class Result
		{
			public Coder.CodePage InputCodePage { get; set; }
			public Compressor.Type CompressorType { get; set; }
			public Coder.CodePage OutputCodePage { get; set; }
		}

		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<CompressDataDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CompressDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		Compressor.Type CompressorType { get { return UIHelper<CompressDataDialog>.GetPropValue<Compressor.Type>(this); } set { UIHelper<CompressDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<CompressDataDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CompressDataDialog>.SetPropValue(this, value); } }

		static CompressDataDialog() { UIHelper<CompressDataDialog>.Register(); }

		CompressDataDialog(Coder.CodePage _codePage, bool compress)
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

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputCodePage = InputCodePage, CompressorType = CompressorType, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage, bool compress)
		{
			var dialog = new CompressDataDialog(codePage, compress) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
