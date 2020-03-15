using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		bool inTransaction = false;
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		TabsWindow oldTabsWindow, newTabsWindow;
		public TabsWindow TabsWindow
		{
			get => newTabsWindow;
			private set
			{
				EnsureInTransaction();
				newTabsWindow = value;
			}
		}

		List<Tab> oldAllTabs, newAllTabs;
		public IReadOnlyList<Tab> AllTabs => newAllTabs;

		public void InsertTab(Tab tab, int? index = null)
		{
			EnsureInTransaction();

			if (tab == null)
				throw new ArgumentNullException();
			if (tab.Tabs != null)
				throw new Exception("Tab already assigned");
			if (newAllTabs.Contains(tab))
				throw new Exception("Tab already in list");

			if (newAllTabs == oldAllTabs)
				newAllTabs = newAllTabs.ToList();
			newAllTabs.Insert(index ?? newAllTabs.Count, tab);
			//TODO tab.TabsWindow = this;

			if (!newActiveTabs.Except(oldAllTabs).Any())
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
			if (tab.Tabs == null)
				throw new Exception("Tab not assigned");
			if (!newAllTabs.Contains(tab))
				throw new Exception("Tab not in list");

			if (newAllTabs == oldAllTabs)
				newAllTabs = newAllTabs.ToList();
			newAllTabs.Remove(tab);
			tab.Tabs = null;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = new HashSet<Tab>(newActiveTabs);
			if (newActiveTabs.Contains(tab))
				newActiveTabs.Remove(tab);
			if (newFocused == tab)
				newFocused = newActiveTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();

			if (newFocused == null)
			{
				newFocused = newAllTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (newFocused != null)
					newActiveTabs.Add(newFocused);
			}
		}

		HashSet<Tab> oldActiveTabs, newActiveTabs;
		public IReadOnlyList<Tab> ActiveTabs => AllTabs.Where(tab => newActiveTabs.Contains(tab)).ToList();

		public void ClearAllActive()
		{
			EnsureInTransaction();
			newActiveTabs = new HashSet<Tab>();
			newFocused = null;
		}

		public void SetActive(Tab tab, bool active = true)
		{
			EnsureInTransaction();
			if (tab == null)
				throw new ArgumentNullException();
			if (newActiveTabs.Contains(tab) == active)
				return;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = new HashSet<Tab>(newActiveTabs);
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
		public Tab Focused
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

		public bool IsActive(Tab tab) => newActiveTabs.Contains(tab);

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

			newTabsWindow = oldTabsWindow;
			newAllTabs = oldAllTabs;
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

			oldTabsWindow = newTabsWindow;
			oldAllTabs = newAllTabs;
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
