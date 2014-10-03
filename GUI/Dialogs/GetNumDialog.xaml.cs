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

		static GetNumDialog()
		{
			UIHelper<GetNumDialog>.Register();
			UIHelper<GetNumDialog>.AddCallback(a => a.Value, (obj, o, n) => obj.Value = Math.Max(obj.MinValue.HasValue ? obj.MinValue.Value : long.MinValue, Math.Min(obj.Value, obj.MaxValue.HasValue ? obj.MaxValue.Value : long.MaxValue)));
		}

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
