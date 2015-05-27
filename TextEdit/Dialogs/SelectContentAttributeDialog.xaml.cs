using System.Windows;
using NeoEdit.GUI.Controls;

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
		public string Attribute { get { return UIHelper<SelectContentAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<SelectContentAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FirstOnly { get { return UIHelper<SelectContentAttributeDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectContentAttributeDialog>.SetPropValue(this, value); } }

		static SelectContentAttributeDialog()
		{
			UIHelper<SelectContentAttributeDialog>.Register();
		}

		SelectContentAttributeDialog()
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
			var dialog = new SelectContentAttributeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
