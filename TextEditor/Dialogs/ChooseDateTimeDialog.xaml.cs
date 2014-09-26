using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class ChooseDateTimeDialog : Window
	{
		[DepProp]
		public DateTime Value { get { return uiHelper.GetPropValue<DateTime>(); } set { uiHelper.SetPropValue(value); } }

		static ChooseDateTimeDialog() { UIHelper<ChooseDateTimeDialog>.Register(); }

		readonly UIHelper<ChooseDateTimeDialog> uiHelper;
		ChooseDateTimeDialog(DateTime value)
		{
			uiHelper = new UIHelper<ChooseDateTimeDialog>(this);
			InitializeComponent();

			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public DateTime? Run(DateTime datetime)
		{
			var dialog = new ChooseDateTimeDialog(datetime);
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.Value;
		}
	}
}
