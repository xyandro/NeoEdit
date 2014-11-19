using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	partial class EncodingDialog
	{
		public class Result : DialogResult
		{
			public StrCoder.CodePage CodePage { get; set; }
			public string LineEndings { get; set; }

			public override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Element(a => a.CodePage),
					neXml.Element(a => a.LineEndings)
				);
			}

			public static Result FromXML(XElement xml)
			{
				return new Result
				{
					CodePage = NEXML<Result>.Element(xml, a => a.CodePage),
					LineEndings = NEXML<Result>.Element(xml, a => a.LineEndings),
				};
			}
		}

		[DepProp]
		StrCoder.CodePage CodePage { get { return UIHelper<EncodingDialog>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string DetectedStr { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string LineEndings { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }

		readonly StrCoder.CodePage Detected;

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		EncodingDialog(StrCoder.CodePage _CodePage, StrCoder.CodePage _Detected, string _LineEndings)
		{
			InitializeComponent();

			codePage.ItemsSource = StrCoder.GetCodePages().ToDictionary(page => page, page => StrCoder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			lineEndings.ItemsSource = new Dictionary<string, string>
			{
				{ "Mixed", "" },
				{ "Windows (CRLF)", "\r\n" },
				{ "Unix (LF)", "\n" },
				{ "Mac (CR)", "\r" },
			};
			lineEndings.DisplayMemberPath = "Key";
			lineEndings.SelectedValuePath = "Value";
			lineEndings.SelectedIndex = 0;

			CodePage = _CodePage;
			if (_Detected == StrCoder.CodePage.None)
				content.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == Grid.GetRow(setDetected)).ToList().ForEach(child => child.Visibility = Visibility.Collapsed);
			else
			{
				Detected = _Detected;
				DetectedStr = StrCoder.GetDescription(Detected);
			}
			if (_LineEndings == null)
				content.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == Grid.GetRow(lineEndings)).ToList().ForEach(child => child.Visibility = Visibility.Collapsed);
			else
				LineEndings = _LineEndings;
		}

		Result response = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			response = new Result { CodePage = CodePage, LineEndings = LineEndings };
			DialogResult = true;
		}

		public static Result Run(StrCoder.CodePage codePage = StrCoder.CodePage.Default, StrCoder.CodePage detected = StrCoder.CodePage.None, string lineEndings = null)
		{
			var dialog = new EncodingDialog(codePage, detected, lineEndings);
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.response;
		}

		void SetDetected(object sender, RoutedEventArgs e)
		{
			CodePage = Detected;
		}
	}
}
