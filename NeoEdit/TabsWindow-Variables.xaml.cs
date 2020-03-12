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
				var notSeen = newTabs.Except(oldTabs).ToList();
				if (notSeen.Any())
					ActiveTabs = notSeen;
				else
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

		int? oldColumns, newColumns;
		public int? Columns
		{
			get => newColumns;
			set => newColumns = value;
		}

		int? oldRows, newRows;
		public int? Rows
		{
			get => newRows;
			set => newRows = value;
		}

		int? oldMaxColumns, newMaxColumns;
		public int? MaxColumns
		{
			get => newMaxColumns;
			set => newMaxColumns = value;
		}

		int? oldMaxRows, newMaxRows;
		public int? MaxRows
		{
			get => newMaxRows;
			set => newMaxRows = value;
		}


		void Rollback()
		{
			newTabs = oldTabs;
			newActiveTabs = oldActiveTabs;
			newFocused = oldFocused;
			newColumns = oldColumns;
			newRows = oldRows;
			newMaxColumns = oldMaxColumns;
			newMaxRows = oldMaxRows;
		}

		void Commit()
		{
			oldTabs = newTabs;
			oldActiveTabs = newActiveTabs;
			oldFocused = newFocused;
			oldColumns = newColumns;
			oldRows = newRows;
			oldMaxColumns = newMaxColumns;
			oldMaxRows = newMaxRows;
		}
	}
}
