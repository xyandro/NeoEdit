using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class LimitDialog : Window
	{
		[DepProp]
		public int MaxSels { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		readonly UIHelper<LimitDialog> uiHelper;
		public LimitDialog()
		{
			uiHelper = new UIHelper<LimitDialog>(this);
			InitializeComponent();

			MaxSels = 1;

			Loaded += (s, e) => maxSels.SelectAll();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Up: MaxSels++; break;
				case Key.Down: MaxSels = Math.Max(1, MaxSels - 1); break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
