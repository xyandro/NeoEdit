using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class SelectLinesDialog : Window
	{
		[DepProp]
		public int LineMult { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IgnoreBlankLines { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static SelectLinesDialog() { UIHelper<SelectLinesDialog>.Register(); }

		readonly UIHelper<SelectLinesDialog> uiHelper;
		SelectLinesDialog()
		{
			uiHelper = new UIHelper<SelectLinesDialog>(this);
			InitializeComponent();

			LineMult = 1;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public bool Run(out int lineMult, out bool ignoreBlankLines)
		{
			lineMult = 1;
			ignoreBlankLines = false;

			var dialog = new SelectLinesDialog();
			if (dialog.ShowDialog() != true)
				return false;

			lineMult = dialog.LineMult;
			ignoreBlankLines = dialog.IgnoreBlankLines;
			return true;
		}
	}
}
