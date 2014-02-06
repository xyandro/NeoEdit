using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class DisplayValue : TextBox
	{
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Type { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static DisplayValue() { UIHelper<DisplayValue>.Register(); }

		readonly UIHelper<DisplayValue> uiHelper;
		public DisplayValue()
		{
			uiHelper = new UIHelper<DisplayValue>(this);
			InitializeComponent();
		}
	}
}
