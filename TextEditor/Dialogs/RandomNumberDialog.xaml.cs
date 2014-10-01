using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class RandomNumberDialog : Window
	{
		[DepProp]
		public int MinValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static RandomNumberDialog() { UIHelper<RandomNumberDialog>.Register(); }

		readonly UIHelper<RandomNumberDialog> uiHelper;
		RandomNumberDialog()
		{
			uiHelper = new UIHelper<RandomNumberDialog>(this);
			InitializeComponent();

			MinValue = 1;
			MaxValue = 1000;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public bool Run(out int minValue, out int maxValue)
		{
			minValue = maxValue = 0;
			var dialog = new RandomNumberDialog();
			if (dialog.ShowDialog() != true)
				return false;

			minValue = Math.Min(dialog.MinValue, dialog.MaxValue);
			maxValue = Math.Max(dialog.MinValue, dialog.MaxValue);
			return true;
		}
	}
}
