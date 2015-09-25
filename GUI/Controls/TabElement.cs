using System.Windows.Controls;

namespace NeoEdit.GUI.Controls
{
	public class TabElement : UserControl
	{
		[DepProp]
		public int ItemOrder { get { return UIHelper<TabElement>.GetPropValue<int>(this); } set { UIHelper<TabElement>.SetPropValue(this, value); } }
		[DepProp]
		public bool Active { get { return UIHelper<TabElement>.GetPropValue<bool>(this); } set { UIHelper<TabElement>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TabElement>.GetPropValue<string>(this); } set { UIHelper<TabElement>.SetPropValue(this, value); } }

		static TabElement() { UIHelper<TabElement>.Register(); }
	}
}
