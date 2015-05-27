using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FindContentAttributeDialog
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public Regex Regex { get; set; }
		}

		[DepProp]
		public string Attribute { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Text { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool AllContents { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WholeWords { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegex { get { return UIHelper<FindContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<FindContentAttributeDialog>.SetPropValue(this, value); } }

		static FindContentAttributeDialog()
		{
			UIHelper<FindContentAttributeDialog>.Register();
		}

		FindContentAttributeDialog()
		{
			InitializeComponent();
			Attribute = "Tag";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrWhiteSpace(Text))
				return;

			var text = Text;
			if (!IsRegex)
				text = Regex.Escape(text);
			if (WholeWords)
				text = @"\b" + text + @"\b";
			if (AllContents)
				text = "^" + text + "$";
			var options = RegexOptions.Compiled | RegexOptions.Singleline;
			if (!MatchCase)
				options |= RegexOptions.IgnoreCase;

			result = new Result { Attribute = Attribute, Regex = new Regex(text, options) };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FindContentAttributeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
