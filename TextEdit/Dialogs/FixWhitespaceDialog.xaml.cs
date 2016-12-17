using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FixWhitespaceDialog
	{
		internal class Result
		{
			public int LineStartTabStop { get; set; }
		}

		[DepProp]
		public int LineStartTabStop { get { return UIHelper<FixWhitespaceDialog>.GetPropValue<int>(this); } set { UIHelper<FixWhitespaceDialog>.SetPropValue(this, value); } }

		static FixWhitespaceDialog() { UIHelper<FixWhitespaceDialog>.Register(); }

		FixWhitespaceDialog()
		{
			InitializeComponent();
			LineStartTabStop = 4;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { LineStartTabStop = LineStartTabStop };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FixWhitespaceDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
