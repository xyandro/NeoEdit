using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class TabsList
	{
		Tabs tabs;
		OrderedHashSet<Tab> allTabs, activeTabs;
		bool activeTabsSorted;
		Tab focused;

		public TabsList(Tabs tabs)
		{
			this.tabs = tabs;
			allTabs = new OrderedHashSet<Tab>();
			activeTabs = new OrderedHashSet<Tab>();
			activeTabsSorted = true;
			focused = null;
		}

		public TabsList(TabsList old)
		{
			tabs = old.tabs;
			allTabs = new OrderedHashSet<Tab>(old.allTabs);
			activeTabs = new OrderedHashSet<Tab>(old.activeTabs);
			activeTabsSorted = old.activeTabsSorted;
			focused = old.focused;
		}

		public IReadOnlyList<Tab> AllTabs => allTabs;

		public void InsertTab(TabsList old, Tab tab, int? index = null)
		{
			if (tab == null)
				throw new ArgumentNullException();
			if (tab.Tabs != null)
				throw new Exception("Tab already assigned");
			if (allTabs.Contains(tab))
				throw new Exception("Tab already in list");

			tabs.AddToTransaction(tab);

			allTabs.Insert(index ?? allTabs.Count, tab);
			tab.Tabs = tabs;

			if (!activeTabs.Where(x => !old.Contains(x)).Any())
				ClearActive();

			activeTabs.Add(tab);
			activeTabsSorted = false;
			if (focused == null)
				focused = tab;
		}

		public void RemoveTab(Tab tab)
		{
			if (tab == null)
				throw new ArgumentNullException();
			if (tab.Tabs == null)
				throw new Exception("Tab not assigned");
			if (!allTabs.Contains(tab))
				throw new Exception("Tab not in list");

			tabs.AddToTransaction(tab);

			allTabs.Remove(tab);
			tab.Tabs = null;

			activeTabs.Remove(tab);
			if (focused == tab)
				focused = activeTabs.LastOrDefault();

			if (focused == null)
			{
				focused = allTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (focused != null)
				{
					activeTabs.Add(focused);
					activeTabsSorted = false;
				}
			}

			if (focused != null)
				tabs.AddToTransaction(focused);
		}

		public IReadOnlyList<Tab> ActiveTabs
		{
			get
			{
				if (!activeTabsSorted)
				{
					activeTabs = new OrderedHashSet<Tab>(allTabs.Where(tab => activeTabs.Contains(tab)));
					activeTabsSorted = true;
				}
				return activeTabs;
			}
		}

		public void ClearActive()
		{
			activeTabs = new OrderedHashSet<Tab>();
			activeTabsSorted = true;
			focused = null;
		}

		public bool Contains(Tab tab) => allTabs.Contains(tab);

		public void SetActive(Tab tab, bool active = true)
		{
			if (tab == null)
				throw new ArgumentNullException();
			if (activeTabs.Contains(tab) == active)
				return;

			if (active)
			{
				tabs.AddToTransaction(tab);

				activeTabs.Add(tab);
				activeTabsSorted = false;
				if (focused == null)
					focused = tab;
			}
			else
			{
				activeTabs.Remove(tab);
				if (focused == tab)
					focused = activeTabs.OrderByDescending(x => x.LastActive).FirstOrDefault();
			}
		}

		public bool IsActive(Tab tab) => activeTabs.Contains(tab);

		public Tab Focused
		{
			get => focused;
			set
			{
				if (!activeTabs.Contains(value))
					throw new Exception("Value not in active set");
				focused = value;
			}
		}
	}
}
