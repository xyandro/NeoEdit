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
		LimitDialog(int maxSels)
		{
			uiHelper = new UIHelper<LimitDialog>(this);
			InitializeComponent();

			MaxSels = maxSels;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public int? Run(int maxSels)
		{
			var dialog = new LimitDialog(maxSels);
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.NumSels;
		}
	}
}
