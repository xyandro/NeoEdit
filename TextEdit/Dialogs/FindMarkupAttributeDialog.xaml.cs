using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FindMarkupAttributeDialog
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public string Value { get; set; }
		}

		[DepProp]
		public string Attribute { get { return UIHelper<FindMarkupAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindMarkupAttributeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<FindMarkupAttributeDialog>.GetPropValue<string>(this); } set { UIHelper<FindMarkupAttributeDialog>.SetPropValue(this, value); } }

		static FindMarkupAttributeDialog()
		{
			UIHelper<FindMarkupAttributeDialog>.Register();
		}

		FindMarkupAttributeDialog()
		{
			InitializeComponent();
			Attribute = "Tag";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Attribute = Attribute, Value = Value };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FindMarkupAttributeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
