using System;
using System.Windows;
using System.Windows.Input;
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

			repeatCount.SelectAll();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Up: RepeatCount++; break;
				case Key.Down: RepeatCount = Math.Max(1, RepeatCount - 1); break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
