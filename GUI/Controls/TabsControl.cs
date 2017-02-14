using System.Windows.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class TabsControl<ItemType, CommandType> : UserControl where ItemType : TabsControl<ItemType, CommandType>
	{
		[DepProp]
		public int ItemOrder { get { return UIHelper<TabsControl<ItemType, CommandType>>.GetPropValue<int>(this); } set { UIHelper<TabsControl<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool Active { get { return UIHelper<TabsControl<ItemType, CommandType>>.GetPropValue<bool>(this); } set { UIHelper<TabsControl<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TabsControl<ItemType, CommandType>>.GetPropValue<string>(this); } set { UIHelper<TabsControl<ItemType, CommandType>>.SetPropValue(this, value); } }

		public Tabs<ItemType, CommandType> TabsParent { get; internal set; }
		public TabsWindow<ItemType, CommandType> WindowParent => TabsParent?.WindowParent;

		static TabsControl() { UIHelper<TabsControl<ItemType, CommandType>>.Register(); }

		public virtual bool Empty() => false;

		public bool CanClose()
		{
			Message.OptionsEnum answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		public virtual bool CanClose(ref Message.OptionsEnum answer) => true;

		public virtual void Closed() { }
	}
}
