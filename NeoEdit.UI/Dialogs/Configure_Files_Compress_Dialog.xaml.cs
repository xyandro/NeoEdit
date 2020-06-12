using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Compress_Dialog
	{
		[DepProp]
		Compressor.Type CompressorType { get { return UIHelper<Configure_Files_Compress_Dialog>.GetPropValue<Compressor.Type>(this); } set { UIHelper<Configure_Files_Compress_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Compress_Dialog() { UIHelper<Configure_Files_Compress_Dialog>.Register(); }

		Configure_Files_Compress_Dialog(bool compress)
		{
			InitializeComponent();

			Title = compress ? "Compress Files" : "Decompress Files";

			compressorType.ItemsSource = Enum.GetValues(typeof(Compressor.Type)).Cast<Compressor.Type>().Where(type => type != Compressor.Type.None).ToList();
			CompressorType = Compressor.Type.GZip;
		}

		Configuration_Files_Compress result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Compress { CompressorType = CompressorType };
			DialogResult = true;
		}

		public static Configuration_Files_Compress Run(Window parent, bool compress)
		{
			var dialog = new Configure_Files_Compress_Dialog(compress) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
