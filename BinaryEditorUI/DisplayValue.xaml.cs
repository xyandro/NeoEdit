using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.BinaryEditorUI
{
	public partial class DisplayValue : TextBox
	{
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public BinaryData.ConverterType Type { get { return uiHelper.GetPropValue<BinaryData.ConverterType>(); } set { uiHelper.SetPropValue(value); } }

		static DisplayValue() { UIHelper<DisplayValue>.Register(); }

		readonly UIHelper<DisplayValue> uiHelper;
		public DisplayValue()
		{
			uiHelper = new UIHelper<DisplayValue>(this);
			InitializeComponent();
			LostFocus += (s, e) => uiHelper.InvalidateBinding(this, TextProperty);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Enter:
					if (!IsReadOnly)
					{
						var data = BinaryData.FromString(Type, Text);
						if (data != null)
							Data.Replace(SelStart, data);
					}
					break;
			}
		}
	}
}
