using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class ChooseDateTimeDialog
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
			return dialog.ShowDialog() == true ? (DateTime?)dialog.Value : null;
		}
	}
}
