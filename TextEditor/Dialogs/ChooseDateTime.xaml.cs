using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class ChooseDateTime : Window
	{
		[DepProp]
		public DateTime Value { get { return uiHelper.GetPropValue<DateTime>(); } set { uiHelper.SetPropValue(value); } }

		static ChooseDateTime() { UIHelper<ChooseDateTime>.Register(); }

		readonly UIHelper<ChooseDateTime> uiHelper;
		ChooseDateTime(DateTime value)
		{
			uiHelper = new UIHelper<ChooseDateTime>(this);
			InitializeComponent();

			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public DateTime? Run(DateTime datetime)
		{
			var dialog = new ChooseDateTime(datetime);
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.Value;
		}
	}
}
