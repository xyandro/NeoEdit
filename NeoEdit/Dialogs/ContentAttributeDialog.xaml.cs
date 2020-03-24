using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class ContentAttributeDialog
	{
		[DepProp]
		public List<string> Attributes { get { return UIHelper<ContentAttributeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<ContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Values { get { return UIHelper<ContentAttributeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<ContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AllContents { get { return UIHelper<ContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<ContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<ContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<ContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Invert { get { return UIHelper<ContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<ContentAttributeDialog>.SetPropValue(this, value); } }

		static ContentAttributeDialog()
		{
			UIHelper<ContentAttributeDialog>.Register();
			UIHelper<ContentAttributeDialog>.AddCallback(a => a.Attribute, (obj, o, n) => obj.UpdateAttrValues());
		}

		readonly List<ParserNode> nodes;
		ContentAttributeDialog(List<ParserNode> nodes)
		{
			InitializeComponent();
			this.nodes = nodes;
			Attributes = ParserNode.GetAvailableAttrs(nodes);
			Attribute = Attributes.FirstOrDefault();
			AllContents = true;
		}

		void UpdateAttrValues()
		{
			Values = ParserNode.GetAvailableValues(nodes, Attribute);
			Value = Values.FirstOrDefault();
		}

		ContentAttributeDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var text = Value;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = $"\\b{text}\\b";
			if (AllContents)
				text = $"^{text}$";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;

			result = new ContentAttributeDialogResult { Attribute = Attribute, Regex = new Regex(text, options), Invert = Invert };
			DialogResult = true;
		}

		public static ContentAttributeDialogResult Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new ContentAttributeDialog(nodes) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
