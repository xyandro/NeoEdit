using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class LimitDialog : Window
	{
		[DepProp]
		public int NumSels { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxSels { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); NumSels = value; } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		readonly UIHelper<LimitDialog> uiHelper;
		public LimitDialog()
		{
			uiHelper = new UIHelper<LimitDialog>(this);
			InitializeComponent();

			MaxSels = 1;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
