using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ContentAttributesDialog
	{
		[DepProp]
		public List<string> Attributes { get { return UIHelper<ContentAttributesDialog>.GetPropValue<List<string>>(this); } set { UIHelper<ContentAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<ContentAttributesDialog>.GetPropValue<string>(this); } set { UIHelper<ContentAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FirstOnly { get { return UIHelper<ContentAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributesDialog>.SetPropValue(this, value); } }

		static ContentAttributesDialog() { UIHelper<ContentAttributesDialog>.Register(); }

		ContentAttributesDialog(List<ParserNode> nodes)
		{
			InitializeComponent();
			Attributes = ParserNode.GetAvailableAttrs(nodes, true);
			Attribute = Attributes.FirstOrDefault();
		}

		ContentAttributesDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new ContentAttributesDialogResult { Attribute = Attribute, FirstOnly = FirstOnly };
			DialogResult = true;
		}

		public static ContentAttributesDialogResult Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new ContentAttributesDialog(nodes) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
