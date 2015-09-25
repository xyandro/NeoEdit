using System;

namespace NeoEdit.GUI.Controls
{
	public class TabsWindow<ItemType> : NEWindow where ItemType : TabsControl
	{
		public Tabs<ItemType> ItemTabs { get; protected set; }

		public static void CreateTab<ClassType>(ItemType item, ClassType classItem = null, bool forceCreate = false) where ClassType : TabsWindow<ItemType>
		{
			if ((classItem == null) && (!forceCreate))
				classItem = UIHelper<ClassType>.GetNewest();

			if (classItem == null)
				classItem = (ClassType)Activator.CreateInstance(typeof(ClassType), true);

			classItem.Activate();

			classItem.ItemTabs.CreateTab(item);
		}
	}
}
