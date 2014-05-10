using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class SelectLinesDialog : TransparentWindow
	{
		[DepProp]
		public int LineMult { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IgnoreBlankLines { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static SelectLinesDialog() { UIHelper<SelectLinesDialog>.Register(); }

		readonly UIHelper<SelectLinesDialog> uiHelper;
		public SelectLinesDialog()
		{
			uiHelper = new UIHelper<SelectLinesDialog>(this);
			InitializeComponent();

			LineMult = 1;
			IgnoreBlankLines = true;

			lineMult.SelectAll();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Up: LineMult++; break;
				case Key.Down: LineMult = Math.Max(1, LineMult - 1); break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
