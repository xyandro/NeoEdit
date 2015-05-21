using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectMarkupAttributeDialog
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public bool FirstOnly { get; set; }
		}

		[DepProp]
		public string Attribute { get { return UIHelper<SelectMarkupAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<SelectMarkupAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FirstOnly { get { return UIHelper<SelectMarkupAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectMarkupAttributeDialog>.SetPropValue(this, value); } }

		static SelectMarkupAttributeDialog()
		{
			UIHelper<SelectMarkupAttributeDialog>.Register();
		}

		SelectMarkupAttributeDialog()
		{
			InitializeComponent();
			Attribute = "Tag";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Attribute = Attribute, FirstOnly = FirstOnly };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new SelectMarkupAttributeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
