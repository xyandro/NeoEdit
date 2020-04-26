using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Image_GIF_Split_Dialog
	{
		[DepProp]
		public string OutputTemplate { get { return UIHelper<Configure_Image_GIF_Split_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Image_GIF_Split_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Image_GIF_Split_Dialog() { UIHelper<Configure_Image_GIF_Split_Dialog>.Register(); }

		Configure_Image_GIF_Split_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			OutputTemplate = @"$@""{directoryname(x)}\{filenamewithoutextension(x)}-{chunk}.png""";
		}

		Configuration_Image_GIF_Split result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Image_GIF_Split { OutputTemplate = OutputTemplate };
			outputTemplate.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Image_GIF_Split Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Image_GIF_Split_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
