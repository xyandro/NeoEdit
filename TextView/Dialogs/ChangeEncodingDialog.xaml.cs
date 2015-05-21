using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextView.Dialogs
{
	partial class ChangeEncodingDialog
	{
		internal class Result
		{
			public string InputFile { get; set; }
			public string OutputFile { get; set; }
			public Coder.CodePage OutputCodePage { get; set; }
		}

		[DepProp]
		public string InputFile { get { return UIHelper<ChangeEncodingDialog>.GetPropValue<string>(this); } set { UIHelper<ChangeEncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string InputCodePage { get { return UIHelper<ChangeEncodingDialog>.GetPropValue<string>(this); } set { UIHelper<ChangeEncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFile { get { return UIHelper<ChangeEncodingDialog>.GetPropValue<string>(this); } set { UIHelper<ChangeEncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputCodePage { get { return UIHelper<ChangeEncodingDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ChangeEncodingDialog>.SetPropValue(this, value); } }

		static ChangeEncodingDialog()
		{
			UIHelper<ChangeEncodingDialog>.Register();
			UIHelper<ChangeEncodingDialog>.AddCallback(a => a.InputFile, (obj, o, n) => obj.SetInputCodePage());
		}

		ChangeEncodingDialog()
		{
			InitializeComponent();
			var encodings = new List<Coder.CodePage> { Coder.CodePage.Default, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE, Coder.CodePage.UTF16BE, Coder.CodePage.UTF32LE, Coder.CodePage.UTF32BE };
			var encodingsList = encodings.Select(codePage => Tuple.Create(codePage, Coder.GetDescription(codePage)));
			outputCodePage.ItemsSource = encodingsList;
			outputCodePage.SelectedValuePath = "Item1";
			outputCodePage.DisplayMemberPath = "Item2";
			OutputCodePage = Coder.CodePage.UTF8;
		}

		void SetInputCodePage()
		{
			InputCodePage = Coder.GetDescription(Coder.CodePageFromBOM(InputFile));
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((String.IsNullOrEmpty(InputFile)) || (String.IsNullOrEmpty(OutputFile)))
				return;
			if (InputFile == OutputFile)
				return;
			result = new Result { InputFile = InputFile, OutputFile = OutputFile, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ChangeEncodingDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void BrowseInputFile(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
			};
			if (dialog.ShowDialog() != true)
				return;

			InputFile = dialog.FileName;
		}

		void BrowseOutputFile(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog
			{
				Filter = "Text files|*.txt|All files|*.*",
				FileName = Path.GetFileName(InputFile),
				InitialDirectory = Path.GetDirectoryName(InputFile),
			};
			if (dialog.ShowDialog() != true)
				return;

			OutputFile = dialog.FileName;
		}
	}
}
