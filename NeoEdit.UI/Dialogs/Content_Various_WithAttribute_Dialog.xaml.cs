using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Content_Various_WithAttribute_Dialog
	{
		[DepProp]
		public List<string> Attributes { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<List<string>>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<string>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Values { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<List<string>>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<string>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AllContents { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Invert { get { return UIHelper<Content_Various_WithAttribute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Content_Various_WithAttribute_Dialog>.SetPropValue(this, value); } }

		static Content_Various_WithAttribute_Dialog()
		{
			UIHelper<Content_Various_WithAttribute_Dialog>.Register();
			UIHelper<Content_Various_WithAttribute_Dialog>.AddCallback(a => a.Attribute, (obj, o, n) => obj.UpdateAttrValues());
		}

		readonly List<ParserNode> nodes;
		Content_Various_WithAttribute_Dialog(List<ParserNode> nodes)
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

		Configuration_Content_Various_WithAttribute result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var text = Value;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = $@"(?:\b(?=\w)|(?=\W)){text}(?:(?<=\w)\b|(?<=\W))";
			if (AllContents)
				text = $"^{text}$";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;

			result = new Configuration_Content_Various_WithAttribute { Attribute = Attribute, Regex = new Regex(text, options), Invert = Invert };
			DialogResult = true;
		}

		public static Configuration_Content_Various_WithAttribute Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new Content_Various_WithAttribute_Dialog(nodes) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
