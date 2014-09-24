using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class RepeatDialog : Window
	{
		[DepProp]
		public int RepeatCount { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		readonly UIHelper<RepeatDialog> uiHelper;
		public RepeatDialog()
		{
			uiHelper = new UIHelper<RepeatDialog>(this);
			InitializeComponent();

			RepeatCount = 1;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
