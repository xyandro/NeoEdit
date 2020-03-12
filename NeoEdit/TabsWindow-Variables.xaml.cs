using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		IReadOnlyList<TextEditorData> oldTabs, newTabs;
		IReadOnlyList<TextEditorData> Tabs
		{
			get => newTabs;
			set
			{
				newTabs = value;
				ActiveTabs = ActiveTabs;
			}
		}

		IReadOnlyList<TextEditorData> oldActiveTabs, newActiveTabs;
		IReadOnlyList<TextEditorData> ActiveTabs
		{
			get => newActiveTabs;
			set
			{
				newActiveTabs = value.Where(tab => Tabs.Contains(tab)).ToList();
				if (!ActiveTabs.Contains(Focused))
					Focused = ActiveTabs.FirstOrDefault();
			}
		}

		TextEditorData oldFocused, newFocused;
		TextEditorData Focused
		{
			get => newFocused;
			set
			{
				if (!Tabs.Contains(value))
					value = null;
				newFocused = value;
				if ((newFocused != null) && (!ActiveTabs.Contains(newFocused)))
					ActiveTabs = new List<TextEditorData> { newFocused };
			}
		}

		void Rollback()
		{
			newTabs = oldTabs;
			newActiveTabs = oldActiveTabs;
			newFocused = oldFocused;
		}

		void Commit()
		{
			oldTabs = newTabs;
			oldActiveTabs = newActiveTabs;
			oldFocused = newFocused;
		}
	}
}
