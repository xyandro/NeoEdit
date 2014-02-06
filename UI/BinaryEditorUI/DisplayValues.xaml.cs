using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class DisplayValues : StackPanel
	{
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowLE { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowBE { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowInt { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowFloat { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowStr { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static DisplayValues() { UIHelper<DisplayValues>.Register(); }

		readonly UIHelper<DisplayValues> uiHelper;
		public DisplayValues()
		{
			uiHelper = new UIHelper<DisplayValues>(this);
			InitializeComponent();
			ShowLE = ShowInt = true;
			ShowBE = ShowFloat = ShowStr = false;
		}
	}
}
