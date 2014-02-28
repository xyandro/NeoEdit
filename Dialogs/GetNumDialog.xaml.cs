using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	public partial class GetNumDialog : Window
	{
		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long? MinValue { get { return uiHelper.GetPropValue<long?>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long? MaxValue { get { return uiHelper.GetPropValue<long?>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long Value { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }

		static GetNumDialog() { UIHelper<GetNumDialog>.Register(); }

		readonly UIHelper<GetNumDialog> uiHelper;
		public GetNumDialog()
		{
			uiHelper = new UIHelper<GetNumDialog>(this);
			InitializeComponent();

			Loaded += (s, e) => value.SelectAll();

			uiHelper.AddCallback(a => a.Value, (o, n) => Value = Math.Max(MinValue.HasValue ? MinValue.Value : long.MinValue, Math.Min(Value, MaxValue.HasValue ? MaxValue.Value : long.MaxValue)));

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
