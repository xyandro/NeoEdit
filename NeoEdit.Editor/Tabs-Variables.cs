using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		void EnsureInTransaction()
		{
			if (state == null)
				throw new Exception("Must start transaction before editing data");
		}

		#region AllTabs
		int nextAllTabsHash = 1, oldAllTabsHash = 1, newAllTabsHash = 1;
		public int AllTabsHash => newAllTabsHash;

		OrderedHashSet<Tab> oldAllTabs, newAllTabs;
		public IEnumerable<Tab> AllTabs => newAllTabs;
		public IEnumerable<ITab> AllITabs => newAllTabs;

		void NewAllTabsUpdated()
		{
			newAllTabsHash = ++nextAllTabsHash;
		}

		public void InsertTab(Tab tab, int? index = null)
		{
			EnsureInTransaction();

			if (tab == null)
				throw new ArgumentNullException();
			if (tab.Tabs != null)
				throw new Exception("Tab already assigned");
			if (newAllTabs.Contains(tab))
				throw new Exception("Tab already in list");

			AddToTransaction(tab);

			if (newAllTabs == oldAllTabs)
				newAllTabs = new OrderedHashSet<Tab>(newAllTabs);
			newAllTabs.Insert(index ?? newAllTabs.Count, tab);
			NewAllTabsUpdated();
			tab.Tabs = this;

			if (!newActiveTabs.Where(x => !oldAllTabs.Contains(x)).Any())
				ClearAllActive();

			newActiveTabs.Add(tab);
			NewActiveTabsUpdated();
			if (newFocused == null)
				newFocused = tab;
		}

		public void RemoveTab(Tab tab)
		{
			EnsureInTransaction();

			if (tab == null)
				throw new ArgumentNullException();
			if (tab.Tabs == null)
				throw new Exception("Tab not assigned");
			if (!newAllTabs.Contains(tab))
				throw new Exception("Tab not in list");

			AddToTransaction(tab);

			if (newAllTabs == oldAllTabs)
				newAllTabs = new OrderedHashSet<Tab>(newAllTabs);
			newAllTabs.Remove(tab);
			NewAllTabsUpdated();
			tab.Tabs = null;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = new OrderedHashSet<Tab>(newActiveTabs);
			if (newActiveTabs.Contains(tab))
			{
				newActiveTabs.Remove(tab);
				NewActiveTabsUpdated();
			}
			if (newFocused == tab)
				if (newActiveTabs.Count == 0)
					newFocused = null;
				else
					newFocused = newActiveTabs[newActiveTabs.Count - 1];

			if (newFocused == null)
			{
				newFocused = newAllTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (newFocused != null)
				{
					newActiveTabs.Add(newFocused);
					NewActiveTabsUpdated();
				}
			}

			if (newFocused != null)
				AddToTransaction(newFocused);
		}
		#endregion

		#region ActiveTabs
		int nextActiveTabsHash = 1, oldActiveTabsHash = 1, newActiveTabsHash = 1;

		OrderedHashSet<Tab> oldActiveTabs, newActiveTabs;

		void NewActiveTabsUpdated()
		{
			newActiveTabsHash = ++nextActiveTabsHash;
			sortedActiveTabs = null;
		}

		public int ActiveTabsHash => newActiveTabsHash;
		public IEnumerable<Tab> UnsortedActiveTabs => newActiveTabs;
		public IEnumerable<ITab> UnsortedActiveITabs => UnsortedActiveTabs;
		public int UnsortedActiveTabsCount => newActiveTabs.Count;
		IReadOnlyList<Tab> sortedActiveTabs;
		public IEnumerable<Tab> SortedActiveTabs
		{
			get
			{
				if (sortedActiveTabs == null)
					sortedActiveTabs = AllTabs.Where(tab => newActiveTabs.Contains(tab)).ToList();
				return sortedActiveTabs;
			}
		}
		public IEnumerable<ITab> SortedActiveITabs => SortedActiveTabs;

		public void ClearAllActive()
		{
			EnsureInTransaction();

			newActiveTabs = new OrderedHashSet<Tab>();
			NewActiveTabsUpdated();
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
				newActiveTabs = new OrderedHashSet<Tab>(newActiveTabs);
			if (active)
			{
				AddToTransaction(tab);

				newActiveTabs.Add(tab);
				NewActiveTabsUpdated();
				if (newFocused == null)
					newFocused = tab;
			}
			else
			{
				newActiveTabs.Remove(tab);
				NewActiveTabsUpdated();
				if (newFocused == tab)
					newFocused = newActiveTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
			}
			sortedActiveTabs = null;
		}

		public bool IsActive(Tab tab) => newActiveTabs.Contains(tab);
		public bool IsActive(ITab tab) => newActiveTabs.Contains(tab);
		#endregion

		#region Focused
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
		public ITab FocusedITab => Focused;
		#endregion

		HashSet<Tab> transactionTabs;
		void AddToTransaction(Tab tab)
		{
			if (transactionTabs.Contains(tab))
				return;
			transactionTabs.Add(tab);
			tab.BeginTransaction(state);
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

		bool oldMacroVisualize = true, newMacroVisualize = true;
		public bool MacroVisualize
		{
			get => newMacroVisualize;
			set
			{
				EnsureInTransaction();
				newMacroVisualize = value;
			}
		}

		IRunTasksDialog newRunTasksDialog;
		public IRunTasksDialog RunTasksDialog
		{
			get
			{
				EnsureInTransaction();
				if (newRunTasksDialog == null)
					lock (this)
						if (newRunTasksDialog == null)
							newRunTasksDialog = state.TabsWindow.CreateIRunTasksDialog();
				return newRunTasksDialog;
			}
		}

		void BeginTransaction(ExecuteState state)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state;
			transactionTabs = new HashSet<Tab>();
			newActiveTabs.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			newAllTabs = oldAllTabs;
			newActiveTabs = oldActiveTabs;
			newAllTabsHash = oldAllTabsHash;
			newActiveTabsHash = oldActiveTabsHash;
			newFocused = oldFocused;
			newRows = oldRows;
			newMaxColumns = oldMaxColumns;
			newMaxRows = oldMaxRows;
			newMacroVisualize = oldMacroVisualize;

			transactionTabs.ForEach(tab => tab.Rollback());
			transactionTabs = null;

			newRunTasksDialog = null;

			state = null;
		}

		void Commit()
		{
			EnsureInTransaction();

			if (oldAllTabs != newAllTabs)
			{
				oldAllTabs.Null(tab => tab.Tabs).Where(tab => !newAllTabs.Contains(tab)).ForEach(tab => tab.Closed());
				oldAllTabs = newAllTabs;
			}
			if (oldActiveTabs != newActiveTabs)
			{
				var now = DateTime.Now;
				newActiveTabs.ForEach(x => x.LastActive = now);
				oldActiveTabs = newActiveTabs;
			}
			oldAllTabsHash = newAllTabsHash;
			oldActiveTabsHash = newActiveTabsHash;
			oldFocused = newFocused;
			oldColumns = newColumns;
			oldRows = newRows;
			oldMaxColumns = newMaxColumns;
			oldMaxRows = newMaxRows;
			oldMacroVisualize = newMacroVisualize;

			transactionTabs.ForEach(tab => tab.Commit());
			transactionTabs = null;

			newRunTasksDialog = null;

			state = null;
		}
	}
}
