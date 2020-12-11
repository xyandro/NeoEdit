using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class File_OpenEncoding_ReopenWithEncoding_Dialog
	{
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<File_OpenEncoding_ReopenWithEncoding_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<File_OpenEncoding_ReopenWithEncoding_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		bool HasBOM { get { return UIHelper<File_OpenEncoding_ReopenWithEncoding_Dialog>.GetPropValue<bool>(this); } set { UIHelper<File_OpenEncoding_ReopenWithEncoding_Dialog>.SetPropValue(this, value); } }

		public static Dictionary<Coder.CodePage, string> CodePages { get; } = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));

		static File_OpenEncoding_ReopenWithEncoding_Dialog() { UIHelper<File_OpenEncoding_ReopenWithEncoding_Dialog>.Register(); }

		File_OpenEncoding_ReopenWithEncoding_Dialog(Coder.CodePage codePage, bool hasBOM)
		{
			InitializeComponent();

			CodePage = codePage;
			HasBOM = hasBOM;
		}

		Configuration_File_OpenEncoding_ReopenWithEncoding result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_File_OpenEncoding_ReopenWithEncoding { CodePage = CodePage, HasBOM = HasBOM };
			DialogResult = true;
		}

		public static Configuration_File_OpenEncoding_ReopenWithEncoding Run(Window parent, Coder.CodePage codePage, bool hasBOM)
		{
			var dialog = new File_OpenEncoding_ReopenWithEncoding_Dialog(codePage, hasBOM) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
