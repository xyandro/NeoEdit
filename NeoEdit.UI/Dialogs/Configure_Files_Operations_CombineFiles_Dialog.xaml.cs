using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Operations_CombineFiles_Dialog
	{
		[DepProp]
		public string InputFiles { get { return UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputFileCount { get { return UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFiles { get { return UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Files_Operations_CombineFiles_Dialog() { UIHelper<Configure_Files_Operations_CombineFiles_Dialog>.Register(); }

		Configure_Files_Operations_CombineFiles_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			InputFiles = "x";
			InputFileCount = "xn";
			OutputFiles = @"$@""{directoryname(xtmin)}\{filenamewithoutextension(xtmin)}-Combine{extension(xtmin)}""";
		}

		Configuration_Files_Operations_CombineFiles result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Operations_CombineFiles { InputFiles = InputFiles, InputFileCount = InputFileCount, OutputFiles = OutputFiles };
			DialogResult = true;
		}

		public static Configuration_Files_Operations_CombineFiles Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Files_Operations_CombineFiles_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
