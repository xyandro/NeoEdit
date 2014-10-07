using System.Windows.Controls;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	partial class DisplayValues : StackPanel
	{
		[DepProp]
		public BinaryEditor ParentWindow { get { return uiHelper.GetPropValue<BinaryEditor>(); } set { uiHelper.SetPropValue(value); } }
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
