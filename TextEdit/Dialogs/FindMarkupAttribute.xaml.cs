using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FindMarkupAttribute
	{
		internal class Result
		{
			public string Attribute { get; set; }
			public string Value { get; set; }
		}

		[DepProp]
		public string Attribute { get { return UIHelper<FindMarkupAttribute>.GetPropValue<string>(this); } set { UIHelper<FindMarkupAttribute>.SetPropValue(this, value); } }
		[DepProp]
		public string Value { get { return UIHelper<FindMarkupAttribute>.GetPropValue<string>(this); } set { UIHelper<FindMarkupAttribute>.SetPropValue(this, value); } }

		static FindMarkupAttribute()
		{
			UIHelper<FindMarkupAttribute>.Register();
		}

		FindMarkupAttribute()
		{
			InitializeComponent();
			Attribute = "tag";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Attribute = Attribute, Value = Value };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FindMarkupAttribute { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
