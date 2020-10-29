using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Advanced_CombineFiles_Dialog
	{
		[DepProp]
		public string InputFiles { get { return UIHelper<Files_Advanced_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Advanced_CombineFiles_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputFileCount { get { return UIHelper<Files_Advanced_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Advanced_CombineFiles_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFiles { get { return UIHelper<Files_Advanced_CombineFiles_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Advanced_CombineFiles_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Files_Advanced_CombineFiles_Dialog() { UIHelper<Files_Advanced_CombineFiles_Dialog>.Register(); }

		Files_Advanced_CombineFiles_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			InputFiles = "x";
			InputFileCount = "xn";
			OutputFiles = @"$@""{directoryname(xtmin)}\{filenamewithoutextension(xtmin)}-Combine{extension(xtmin)}""";
		}

		Configuration_Files_Advanced_CombineFiles result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Advanced_CombineFiles { InputFiles = InputFiles, InputFileCount = InputFileCount, OutputFiles = OutputFiles };
			DialogResult = true;
		}

		public static Configuration_Files_Advanced_CombineFiles Run(Window parent, NEVariables variables)
		{
			var dialog = new Files_Advanced_CombineFiles_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
