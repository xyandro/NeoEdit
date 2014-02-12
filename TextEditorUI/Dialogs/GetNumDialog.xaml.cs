using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI.Dialogs
{
	public partial class GetNumDialog : Window
	{
		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int? MinValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int? MaxValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Value
		{
			get { return uiHelper.GetPropValue<int>(); }
			set
			{
				value = Math.Max(MinValue.HasValue ? MinValue.Value : int.MinValue, Math.Min(value, MaxValue.HasValue ? MaxValue.Value : int.MaxValue));
				uiHelper.SetPropValue(value);
			}
		}

		static GetNumDialog() { UIHelper<GetNumDialog>.Register(); }

		readonly UIHelper<GetNumDialog> uiHelper;
		public GetNumDialog()
		{
			uiHelper = new UIHelper<GetNumDialog>(this);
			InitializeComponent();

			Loaded += (s, e) => value.SelectAll();

			okClick.Click += (s, e) =>
			{
				if (Validation.GetHasError(value))
					return;
				DialogResult = true;
			};
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Up: ++Value; break;
				case Key.Down: --Value; break;
				default: e.Handled = false; break;
			}
		}
	}
}
