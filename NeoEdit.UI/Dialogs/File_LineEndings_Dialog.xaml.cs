using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class File_LineEndings_Dialog
	{
		[DepProp]
		string LineEndings { get { return UIHelper<File_LineEndings_Dialog>.GetPropValue<string>(this); } set { UIHelper<File_LineEndings_Dialog>.SetPropValue(this, value); } }

		static File_LineEndings_Dialog() { UIHelper<File_LineEndings_Dialog>.Register(); }

		File_LineEndings_Dialog(string _LineEndings)
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

		Configuration_File_LineEndings result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (LineEndings == "")
				return;
			result = new Configuration_File_LineEndings { LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Configuration_File_LineEndings Run(Window parent, string lineEndings)
		{
			var dialog = new File_LineEndings_Dialog(lineEndings) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
