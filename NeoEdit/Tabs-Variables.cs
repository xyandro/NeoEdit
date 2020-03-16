﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		void EnsureInTransaction()
		{
			if (state == null)
				throw new Exception("Must start transaction before editing data");
		}

		OrderedHashSet<Tab> oldAllTabs, newAllTabs;
		public IReadOnlyOrderedHashSet<Tab> AllTabs => newAllTabs;

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
			sortedActiveTabs = null;
			tab.Tabs = this;

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

			AddToTransaction(tab);

			if (newAllTabs == oldAllTabs)
				newAllTabs = new OrderedHashSet<Tab>(newAllTabs);
			newAllTabs.Remove(tab);
			sortedActiveTabs = null;
			tab.Tabs = null;

			if (newActiveTabs == oldActiveTabs)
				newActiveTabs = new OrderedHashSet<Tab>(newActiveTabs);
			if (newActiveTabs.Contains(tab))
				newActiveTabs.Remove(tab);
			if (newFocused == tab)
				if (newActiveTabs.Count == 0)
					newFocused = null;
				else
					newFocused = newActiveTabs[newActiveTabs.Count - 1];

			if (newFocused == null)
			{
				newFocused = newAllTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (newFocused != null)
					newActiveTabs.Add(newFocused);
			}

			if (newFocused != null)
				AddToTransaction(newFocused);
		}

		OrderedHashSet<Tab> oldActiveTabs, newActiveTabs;
		public IReadOnlyOrderedHashSet<Tab> UnsortedActiveTabs => newActiveTabs;
		IReadOnlyList<Tab> sortedActiveTabs;
		public IReadOnlyList<Tab> SortedActiveTabs
		{
			get
			{
				if (sortedActiveTabs == null)
					sortedActiveTabs = AllTabs.Intersect(newActiveTabs).ToList();
				return sortedActiveTabs;
			}
		}

		public void ClearAllActive()
		{
			EnsureInTransaction();
			newActiveTabs = new OrderedHashSet<Tab>();
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
				if (newFocused == null)
					newFocused = tab;
			}
			else
			{
				newActiveTabs.Remove(tab);
				if (newFocused == tab)
					newFocused = newActiveTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
			}
			sortedActiveTabs = null;
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

		void AddToTransaction(Tab tab)
		{
			if (transactionTabs.Contains(tab))
				return;
			transactionTabs.Add(tab);
			tab.BeginTransaction(state);
		}

		HashSet<Tab> transactionTabs;
		void BeginTransaction(ExecuteState state)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state;
			transactionTabs = new HashSet<Tab>();
			UnsortedActiveTabs.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			newAllTabs = oldAllTabs;
			newActiveTabs = oldActiveTabs;
			newFocused = oldFocused;
			newColumns = oldColumns;
			newRows = oldRows;
			newMaxColumns = oldMaxColumns;
			newMaxRows = oldMaxRows;

			transactionTabs.ForEach(tab => tab.Rollback());
			transactionTabs = null;

			state = null;
		}

		void Commit()
		{
			EnsureInTransaction();

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

			transactionTabs.ForEach(tab => tab.Commit());
			transactionTabs = null;

			state = null;
		}
	}
}
