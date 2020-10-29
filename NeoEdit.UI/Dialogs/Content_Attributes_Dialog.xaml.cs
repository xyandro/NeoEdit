using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Content_Attributes_Dialog
	{
		[DepProp]
		public List<string> Attributes { get { return UIHelper<Content_Attributes_Dialog>.GetPropValue<List<string>>(this); } set { UIHelper<Content_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<Content_Attributes_Dialog>.GetPropValue<string>(this); } set { UIHelper<Content_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FirstOnly { get { return UIHelper<Content_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Attributes_Dialog>.SetPropValue(this, value); } }

		static Content_Attributes_Dialog() { UIHelper<Content_Attributes_Dialog>.Register(); }

		Content_Attributes_Dialog(List<ParserNode> nodes)
		{
			InitializeComponent();
			Attributes = ParserNode.GetAvailableAttrs(nodes, true);
			Attribute = Attributes.FirstOrDefault();
		}

		Configuration_Content_Attributes result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Content_Attributes { Attribute = Attribute, FirstOnly = FirstOnly };
			DialogResult = true;
		}

		public static Configuration_Content_Attributes Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new Content_Attributes_Dialog(nodes) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
