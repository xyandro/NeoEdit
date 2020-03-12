using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		bool inTransaction = false;
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		IReadOnlyList<TextEditorData> oldTabs, newTabs;
		IReadOnlyList<TextEditorData> Tabs
		{
			get => newTabs;
			set
			{
				EnsureInTransaction();
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
				EnsureInTransaction();
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
				EnsureInTransaction();
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
			set
			{
				EnsureInTransaction();
				newColumns = value;
			}
		}

		int? oldRows, newRows;
		public int? Rows
		{
			get => newRows;
			set
			{
				EnsureInTransaction();
				newRows = value;
			}
		}

		int? oldMaxColumns, newMaxColumns;
		public int? MaxColumns
		{
			get => newMaxColumns;
			set
			{
				EnsureInTransaction();
				newMaxColumns = value;
			}
		}

		int? oldMaxRows, newMaxRows;
		public int? MaxRows
		{
			get => newMaxRows;
			set
			{
				EnsureInTransaction();
				newMaxRows = value;
			}
		}


		void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
		}

		void Rollback()
		{
			EnsureInTransaction();

			newTabs = oldTabs;
			newActiveTabs = oldActiveTabs;
			newFocused = oldFocused;
			newColumns = oldColumns;
			newRows = oldRows;
			newMaxColumns = oldMaxColumns;
			newMaxRows = oldMaxRows;

			inTransaction = false;
		}

		void Commit()
		{
			EnsureInTransaction();

			oldTabs = newTabs;
			oldActiveTabs = newActiveTabs;
			oldFocused = newFocused;
			oldColumns = newColumns;
			oldRows = newRows;
			oldMaxColumns = newMaxColumns;
			oldMaxRows = newMaxRows;

			inTransaction = false;
		}
	}
}
