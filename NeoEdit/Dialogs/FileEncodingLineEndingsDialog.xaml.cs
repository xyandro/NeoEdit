using System.Collections.Generic;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class FileEncodingLineEndingsDialog
	{
		public class Result
		{
			public string LineEndings { get; set; }
		}

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

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (LineEndings == "")
				return;
			result = new Result { LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Result Run(Window parent, string lineEndings)
		{
			var dialog = new FileEncodingLineEndingsDialog(lineEndings) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}
