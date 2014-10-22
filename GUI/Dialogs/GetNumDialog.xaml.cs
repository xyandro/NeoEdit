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
		public string Text { get { return UIHelper<GetNumDialog>.GetPropValue<string>(this); } set { UIHelper<GetNumDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? MinValue { get { return UIHelper<GetNumDialog>.GetPropValue<long?>(this); } set { UIHelper<GetNumDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long? MaxValue { get { return UIHelper<GetNumDialog>.GetPropValue<long?>(this); } set { UIHelper<GetNumDialog>.SetPropValue(this, value); } }
		[DepProp]
		public long Value { get { return UIHelper<GetNumDialog>.GetPropValue<long>(this); } set { UIHelper<GetNumDialog>.SetPropValue(this, value); } }

		static GetNumDialog()
		{
			UIHelper<GetNumDialog>.Register();
			UIHelper<GetNumDialog>.AddCallback(a => a.Value, (obj, o, n) => obj.Value = Math.Max(obj.MinValue.HasValue ? obj.MinValue.Value : long.MinValue, Math.Min(obj.Value, obj.MaxValue.HasValue ? obj.MaxValue.Value : long.MaxValue)));
		}

		public GetNumDialog()
		{
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
