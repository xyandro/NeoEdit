using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class RepeatDialog
	{
		internal class Result
		{
			public int RepeatCount { get; set; }
			public bool SelectAll { get; set; }
		}

		[DepProp]
		public int RepeatCount { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectAll { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		readonly UIHelper<RepeatDialog> uiHelper;
		RepeatDialog(bool selectAll)
		{
			uiHelper = new UIHelper<RepeatDialog>(this);
			InitializeComponent();

			RepeatCount = 1;
			SelectAll = selectAll;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { RepeatCount = RepeatCount, SelectAll = SelectAll };
			DialogResult = true;
		}

		static public Result Run(bool selectAll)
		{
			var dialog = new RepeatDialog(selectAll);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
