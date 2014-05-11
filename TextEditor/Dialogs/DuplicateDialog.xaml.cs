using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class DuplicateDialog : Window
	{
		[DepProp]
		public int DupCount { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static DuplicateDialog() { UIHelper<DuplicateDialog>.Register(); }

		readonly UIHelper<DuplicateDialog> uiHelper;
		public DuplicateDialog()
		{
			uiHelper = new UIHelper<DuplicateDialog>(this);
			InitializeComponent();

			DupCount = 1;

			dupCount.SelectAll();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Up: DupCount++; break;
				case Key.Down: DupCount = Math.Max(1, DupCount - 1); break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
