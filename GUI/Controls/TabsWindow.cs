using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class TabsWindow<ItemType, CommandType> : NEWindow where ItemType : TabsControl<ItemType, CommandType>
	{
		[DepProp]
		public Tabs<ItemType, CommandType> ItemTabs { get { return UIHelper<TabsWindow<ItemType, CommandType>>.GetPropValue<Tabs<ItemType, CommandType>>(this); } protected set { UIHelper<TabsWindow<ItemType, CommandType>>.SetPropValue(this, value); } }

		static TabsWindow()
		{
			UIHelper<TabsWindow<ItemType, CommandType>>.Register();
			UIHelper<TabsWindow<ItemType, CommandType>>.AddCallback(a => a.ItemTabs, (obj, o, n) => obj.ItemTabs.WindowParent = obj);
		}

		protected bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		protected bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		protected bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

		public static Tuple<ItemType, Window> CreateTab<ClassType>(ItemType item, ClassType classItem = null, bool forceCreate = false) where ClassType : TabsWindow<ItemType, CommandType>
		{
			if ((classItem == null) && (!forceCreate))
				classItem = UIHelper<ClassType>.GetNewest();

			if (classItem == null)
				classItem = (ClassType)Activator.CreateInstance(typeof(ClassType), true);

			return new Tuple<ItemType, Window>(classItem.ItemTabs.CreateTab(item), classItem);
		}

		public void Remove(ItemType item, bool closeIfLast = false)
		{
			ItemTabs.Remove(item);
			if ((closeIfLast) && (ItemTabs.Items.Count == 0))
				Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = Message.OptionsEnum.None;
			var topMost = ItemTabs.TopMost;
			foreach (var item in ItemTabs.Items)
			{
				ItemTabs.TopMost = item;
				if (!item.CanClose(ref answer))
				{
					e.Cancel = true;
					return;
				}
			}
			ItemTabs.TopMost = topMost;
			ItemTabs.Items.ToList().ForEach(item => item.Closed());
			base.OnClosing(e);
		}

		public virtual bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown) => false;
		public virtual bool HandleText(string text) => false;
		public virtual bool HandleCommand(CommandType command, bool shiftDown, object dialogResult, bool? multiStatus) => false;
	}
}
