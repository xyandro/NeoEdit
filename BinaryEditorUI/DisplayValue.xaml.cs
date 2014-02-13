using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Data;

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
		public Coder.Type Type { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

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
						var data = Coder.StringToBytes(Text, Type);
						if (data != null)
							Data.Replace(SelStart, data);
					}
					break;
			}
		}
	}
}
