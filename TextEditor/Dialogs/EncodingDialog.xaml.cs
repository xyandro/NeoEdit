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
			public Coder.Type Encoding { get; set; }
			public bool BOM { get; set; }
			public string LineEndings { get; set; }
		}

		[DepProp]
		public Coder.Type Encoding { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool BOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string LineEndings { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		class Tester
		{
			public string Display { get; set; }
			public Coder.Type Encoding { get; set; }
		}

		readonly UIHelper<EncodingDialog> uiHelper;
		EncodingDialog()
		{
			uiHelper = new UIHelper<EncodingDialog>(this);
			InitializeComponent();

			encoding.ItemsSource = Coder.GetEncodingTypes();
			encoding.DisplayMemberPath = "Key";
			encoding.SelectedValuePath = "Value";

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
			response = new Response { Encoding = Encoding, BOM = BOM, LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Response Run(Coder.Type encoding, bool bom, string lineEndings)
		{
			var dialog = new EncodingDialog { Encoding = encoding, BOM = bom, LineEndings = lineEndings };
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.response;
		}
	}
}
