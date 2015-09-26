using System.Windows.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class TabsControl<ItemType> : UserControl where ItemType : TabsControl<ItemType>
	{
		[DepProp]
		public int ItemOrder { get { return UIHelper<TabsControl<ItemType>>.GetPropValue<int>(this); } set { UIHelper<TabsControl<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool Active { get { return UIHelper<TabsControl<ItemType>>.GetPropValue<bool>(this); } set { UIHelper<TabsControl<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TabsControl<ItemType>>.GetPropValue<string>(this); } set { UIHelper<TabsControl<ItemType>>.SetPropValue(this, value); } }

		public Tabs<ItemType> TabsParent { get; internal set; }
		public TabsWindow<ItemType> WindowParent { get { return TabsParent.WindowParent; } }

		static TabsControl() { UIHelper<TabsControl<ItemType>>.Register(); }

		public virtual bool Empty()
		{
			return false;
		}

		public bool CanClose()
		{
			Message.OptionsEnum answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		public virtual bool CanClose(ref Message.OptionsEnum answer)
		{
			return true;
		}

		public virtual void Closed()
		{
		}
	}
}
