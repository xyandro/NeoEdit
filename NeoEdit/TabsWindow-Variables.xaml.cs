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

		IReadOnlyList<Tab> oldTabs, newTabs;
		IReadOnlyList<Tab> Tabs
		{
			get => newTabs;
			set
			{
				EnsureInTransaction();

				Tabs.ForEach(x => x.TabsWindow = null);

				newTabs = value;

				Tabs.ForEach(x => x.TabsWindow = this);

				var notSeen = newTabs.Except(oldTabs).ToList();
				if (notSeen.Any())
					ActiveTabs = notSeen;
				else
					ActiveTabs = ActiveTabs;
			}
		}

		IReadOnlyList<Tab> oldActiveTabs, newActiveTabs;
		IReadOnlyList<Tab> ActiveTabs
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

		Tab oldFocused, newFocused;
		Tab Focused
		{
			get => newFocused;
			set
			{
				EnsureInTransaction();
				if (!Tabs.Contains(value))
					value = null;
				newFocused = value;
				if ((newFocused != null) && (!ActiveTabs.Contains(newFocused)))
					ActiveTabs = new List<Tab> { newFocused };
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
