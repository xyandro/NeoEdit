using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class EncodingDialog
	{
		public class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public string LineEndings { get; set; }
		}

		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<EncodingDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string DetectedStr { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string LineEndings { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }

		readonly Coder.CodePage Detected;

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		EncodingDialog(Coder.CodePage _CodePage, Coder.CodePage _Detected, string _LineEndings)
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

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

			CodePage = _CodePage;
			if (_Detected == Coder.CodePage.None)
				content.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == Grid.GetRow(setDetected)).ToList().ForEach(child => child.Visibility = Visibility.Collapsed);
			else
			{
				Detected = _Detected;
				DetectedStr = Coder.GetDescription(Detected);
			}
			if (_LineEndings == null)
				content.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == Grid.GetRow(lineEndings)).ToList().ForEach(child => child.Visibility = Visibility.Collapsed);
			else
				LineEndings = _LineEndings;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { CodePage = CodePage, LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage = Coder.CodePage.Default, Coder.CodePage detected = Coder.CodePage.None, string lineEndings = null)
		{
			var dialog = new EncodingDialog(codePage, detected, lineEndings) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}

		void SetDetected(object sender, RoutedEventArgs e) => CodePage = Detected;
	}
}
