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

		List<Tab> oldTabs, newTabs;
		IReadOnlyList<Tab> Tabs => newTabs;

		void InsertTab(Tab tab, int? index = null)
		{
			EnsureInTransaction();

			if (tab == null)
				throw new ArgumentNullException();
			if (tab.TabsWindow != null)
				throw new Exception("Tab already assigned");
			if (newTabs.Contains(tab))
				throw new Exception("Tab already in list");

			if (newTabs == oldTabs)
				newTabs = newTabs.ToList();
			newTabs.Insert(index ?? newTabs.Count, tab);
			tab.TabsWindow = this;

			if (!newActiveTabs.Except(oldTabs).Any())
				ClearAllActive();

			newActiveTabs.Add(tab);
			if (newFocused == null)
				newFocused = tab;
		}

		void RemoveTab(Tab tab)
		{
			EnsureInTransaction();

			if (tab == null)
				throw new ArgumentNullException();
			if (tab.TabsWindow == null)
				throw new Exception("Tab not assigned");
			if (!newTabs.Contains(tab))
				throw new Exception("Tab not in list");

			if (newTabs == oldTabs)
				newTabs = newTabs.ToList();
			newTabs.Remove(tab);
			tab.TabsWindow = null;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = newActiveTabs.ToList();
			if (newActiveTabs.Contains(tab))
				newActiveTabs.Remove(tab);
			if (newFocused == tab)
				newFocused = newActiveTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();

			if (newFocused == null)
			{
				newFocused = newTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (newFocused != null)
					newActiveTabs.Add(newFocused);
			}
		}

		List<Tab> oldActiveTabs, newActiveTabs;
		IReadOnlyList<Tab> ActiveTabs => newActiveTabs;

		void ClearAllActive()
		{
			EnsureInTransaction();
			newActiveTabs = new List<Tab>();
			newFocused = null;
		}

		void SetActive(Tab tab, bool active = true)
		{
			EnsureInTransaction();
			if (tab == null)
				throw new ArgumentNullException();
			if (newActiveTabs.Contains(tab) == active)
				return;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = newActiveTabs.ToList();
			if (active)
			{
				newActiveTabs.Add(tab);
				if (newFocused == null)
					newFocused = tab;
			}
			else
			{
				newActiveTabs.Remove(tab);
				if (newFocused == tab)
					newFocused = newActiveTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
			}
		}

		Tab oldFocused, newFocused;
		Tab Focused
		{
			get => newFocused;
			set
			{
				EnsureInTransaction();
				if (!newActiveTabs.Contains(value))
					throw new Exception("Value not in active set");
				newFocused = value;
			}
		}

		int? oldColumns = 1, newColumns = 1;
		public int? Columns
		{
			get => newColumns;
			set
			{
				EnsureInTransaction();
				newColumns = value;
			}
		}

		int? oldRows = 1, newRows = 1;
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
			if (oldActiveTabs != newActiveTabs)
			{
				var now = DateTime.Now;
				newActiveTabs.ForEach(x => x.LastActive = now);
			}
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
