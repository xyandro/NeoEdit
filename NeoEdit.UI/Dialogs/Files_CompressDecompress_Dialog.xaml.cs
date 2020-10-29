using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_CompressDecompress_Dialog
	{
		[DepProp]
		Compressor.Type CompressorType { get { return UIHelper<Files_CompressDecompress_Dialog>.GetPropValue<Compressor.Type>(this); } set { UIHelper<Files_CompressDecompress_Dialog>.SetPropValue(this, value); } }

		static Files_CompressDecompress_Dialog() { UIHelper<Files_CompressDecompress_Dialog>.Register(); }

		Files_CompressDecompress_Dialog(bool compress)
		{
			InitializeComponent();

			Title = compress ? "Compress Files" : "Decompress Files";

			compressorType.ItemsSource = Enum.GetValues(typeof(Compressor.Type)).Cast<Compressor.Type>().Where(type => type != Compressor.Type.None).ToList();
			CompressorType = Compressor.Type.GZip;
		}

		Configuration_Files_CompressDecompress result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_CompressDecompress { CompressorType = CompressorType };
			DialogResult = true;
		}

		public static Configuration_Files_CompressDecompress Run(Window parent, bool compress)
		{
			var dialog = new Files_CompressDecompress_Dialog(compress) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
