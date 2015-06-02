using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.Parsing;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FindContentAttributeDialog
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public Regex Regex { get; set; }
			public bool Invert { get; set; }
		}

		[DepProp]
		public List<string> Attributes { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Values { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AllContents { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Invert { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }

		static FindContentAttributeDialog()
		{
			UIHelper<FindContentAttributeDialog>.Register();
			UIHelper<FindContentAttributeDialog>.AddCallback(a => a.Attribute, (obj, o, n) => obj.UpdateAttrValues());
		}

		readonly List<ParserNode> nodes;
		FindContentAttributeDialog(List<ParserNode> nodes)
		{
			InitializeComponent();
			this.nodes = nodes;
			Attributes = Parser.GetAvailableAttrs(nodes);
			Attribute = Attributes.FirstOrDefault();
			AllContents = true;
		}

		void UpdateAttrValues()
		{
			Values = Parser.GetAvailableValues(nodes, Attribute);
			Value = Values.FirstOrDefault();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var text = Value;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = @"\b" + text + @"\b";
			if (AllContents)
				text = "^" + text + "$";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;

			result = new Result { Attribute = Attribute, Regex = new Regex(text, options), Invert = Invert };
			DialogResult = true;
		}

		public static Result Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new FindContentAttributeDialog(nodes) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
