using System.Windows.Controls;

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

		static TabsControl()
		{
			UIHelper<TabsControl<ItemType, CommandType>>.Register();
			UIHelper<TabsControl<ItemType, CommandType>>.AddCallback(a => a.Active, (obj, o, n) => obj.TabsParent?.NotifyActiveChanged());
		}

		public virtual bool Empty() => false;

		public bool CanClose() => CanClose(new AnswerResult());

		public virtual bool CanClose(AnswerResult answer) => true;

		public virtual void Closed() { }
	}
}
