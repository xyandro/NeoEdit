using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RepeatDialog
	{
		internal class Result
		{
			public int RepeatCount { get; set; }
			public bool ClipboardValue { get; set; }
			public bool SelectRepetitions { get; set; }
		}

		[DepProp]
		public int RepeatCount { get { return UIHelper<RepeatDialog>.GetPropValue<int>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ClipboardValue { get { return UIHelper<RepeatDialog>.GetPropValue<bool>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectRepetitions { get { return UIHelper<RepeatDialog>.GetPropValue<bool>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		RepeatDialog(bool selectRepetitions)
		{
			InitializeComponent();

			RepeatCount = 1;
			ClipboardValue = false;
			SelectRepetitions = selectRepetitions;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { RepeatCount = RepeatCount, ClipboardValue = ClipboardValue, SelectRepetitions = SelectRepetitions };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool selectRepetitions)
		{
			var dialog = new RepeatDialog(selectRepetitions) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
