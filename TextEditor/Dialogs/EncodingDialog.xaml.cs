using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class EncodingDialog
	{
		internal class Response
		{
			public StrCoder.CodePage CodePage { get; set; }
			public string LineEndings { get; set; }
		}

		[DepProp]
		public StrCoder.CodePage CodePage { get { return UIHelper<EncodingDialog>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEndings { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		class Tester
		{
			public string Display { get; set; }
			public StrCoder.CodePage CodePage { get; set; }
		}

		EncodingDialog()
		{
			InitializeComponent();

			codePage.ItemsSource = StrCoder.GetEncodingTypes();
			codePage.SelectedValuePath = "Item1";
			codePage.DisplayMemberPath = "Item2";

			lineEndings.ItemsSource = new Dictionary<string, string>
			{
				{ "Mixed", null },
				{ "Windows (CRLF)", "\r\n" },
				{ "Unix (LF)", "\n" },
				{ "Mac (CR)", "\r" },
			};
			lineEndings.DisplayMemberPath = "Key";
			lineEndings.SelectedValuePath = "Value";
			lineEndings.SelectedIndex = 0;
		}

		Response response = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			response = new Response { CodePage = CodePage, LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Response Run(StrCoder.CodePage codePage, string lineEndings)
		{
			var dialog = new EncodingDialog { CodePage = codePage, LineEndings = lineEndings };
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.response;
		}
	}
}
