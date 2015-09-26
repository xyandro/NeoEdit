using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class TabsWindow<ItemType> : NEWindow where ItemType : TabsControl<ItemType>
	{
		[DepProp]
		public Tabs<ItemType> ItemTabs { get { return UIHelper<TabsWindow<ItemType>>.GetPropValue<Tabs<ItemType>>(this); } protected set { UIHelper<TabsWindow<ItemType>>.SetPropValue(this, value); } }

		static TabsWindow()
		{
			UIHelper<TabsWindow<ItemType>>.Register();
			UIHelper<TabsWindow<ItemType>>.AddCallback(a => a.ItemTabs, (obj, o, n) => obj.ItemTabs.WindowParent = obj);
		}

		protected bool shiftDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); } }
		protected bool controlDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Control); } }
		protected bool altDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Alt); } }

		public static void CreateTab<ClassType>(ItemType item, ClassType classItem = null, bool forceCreate = false) where ClassType : TabsWindow<ItemType>
		{
			if ((classItem == null) && (!forceCreate))
				classItem = UIHelper<ClassType>.GetNewest();

			if (classItem == null)
				classItem = (ClassType)Activator.CreateInstance(typeof(ClassType), true);

			classItem.Activate();

			classItem.ItemTabs.CreateTab(item);
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
	}
}
