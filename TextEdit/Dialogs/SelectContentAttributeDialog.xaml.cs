using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Content;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectContentAttributeDialog
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public bool FirstOnly { get; set; }
		}

		[DepProp]
		public List<string> Attributes { get { return UIHelper<SelectContentAttributeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<SelectContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Attribute { get { return UIHelper<SelectContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<SelectContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FirstOnly { get { return UIHelper<SelectContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectContentAttributeDialog>.SetPropValue(this, value); } }

		static SelectContentAttributeDialog() { UIHelper<SelectContentAttributeDialog>.Register(); }

		SelectContentAttributeDialog(List<ParserNode> nodes)
		{
			InitializeComponent();
			Attributes = Parser.GetAvailableAttrs(nodes, true);
			Attribute = Attributes.FirstOrDefault();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Attribute = Attribute, FirstOnly = FirstOnly };
			DialogResult = true;
		}

		public static Result Run(Window parent, List<ParserNode> nodes)
		{
			var dialog = new SelectContentAttributeDialog(nodes) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
