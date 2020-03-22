using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class FileEncodingLineEndingsDialog
	{
		[DepProp]
		string LineEndings { get { return UIHelper<FileEncodingLineEndingsDialog>.GetPropValue<string>(this); } set { UIHelper<FileEncodingLineEndingsDialog>.SetPropValue(this, value); } }

		static FileEncodingLineEndingsDialog() { UIHelper<FileEncodingLineEndingsDialog>.Register(); }

		FileEncodingLineEndingsDialog(string _LineEndings)
		{
			InitializeComponent();

			lineEndings.ItemsSource = new Dictionary<string, string>
			{
				["Mixed"] = "",
				["Windows (CRLF)"] = "\r\n",
				["Unix (LF)"] = "\n",
				["Mac (CR)"] = "\r",
			};
			lineEndings.DisplayMemberPath = "Key";
			lineEndings.SelectedValuePath = "Value";
			lineEndings.SelectedIndex = 0;

			LineEndings = _LineEndings;
		}

		FileEncodingLineEndingsDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (LineEndings == "")
				return;
			result = new FileEncodingLineEndingsDialogResult { LineEndings = LineEndings };
			DialogResult = true;
		}

		public static FileEncodingLineEndingsDialogResult Run(Window parent, string lineEndings)
		{
			var dialog = new FileEncodingLineEndingsDialog(lineEndings) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
