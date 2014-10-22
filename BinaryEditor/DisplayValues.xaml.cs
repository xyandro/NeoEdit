using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	partial class DisplayValues : StackPanel
	{
		[DepProp]
		public BinaryEditor ParentWindow { get { return UIHelper<DisplayValues>.GetPropValue<BinaryEditor>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<DisplayValues>.GetPropValue<bool>(this); } set { UIHelper<DisplayValues>.SetPropValue(this, value); } }

		static DisplayValues() { UIHelper<DisplayValues>.Register(); }

		public DisplayValues()
		{
			InitializeComponent();
			ShowLE = ShowInt = true;
			ShowBE = ShowFloat = ShowStr = false;
		}
	}
}
