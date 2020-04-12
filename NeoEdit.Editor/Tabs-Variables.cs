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

		TabsList oldTabsList, newTabsList;
		TabsList GetUpdateTabsList()
		{
			EnsureInTransaction();
			if (newTabsList == oldTabsList)
				newTabsList = new TabsList(newTabsList);
			return newTabsList;
		}

		public int AllTabsHash => newTabsList.AllTabsHash;

		public IReadOnlyList<Tab> AllTabs => newTabsList.AllTabs;
		public IReadOnlyList<ITab> AllITabs => newTabsList.AllTabs;

		public void InsertTab(Tab tab, int? index = null)
		{
			lock (this)
				GetUpdateTabsList().InsertTab(oldTabsList, tab, index);
		}

		public void RemoveTab(Tab tab)
		{
			lock (this)
				GetUpdateTabsList().RemoveTab(tab);
		}

		public int ActiveTabsHash => newTabsList.ActiveTabsHash;
		public IReadOnlyList<Tab> ActiveTabs => newTabsList.ActiveTabs;
		public IReadOnlyList<ITab> ActiveITabs => newTabsList.ActiveTabs;

		public void ClearAllActive() => GetUpdateTabsList().ClearActive();

		public void SetActive(Tab tab, bool active = true) => GetUpdateTabsList().SetActive(tab, active);

		public bool IsActive(Tab tab) => newTabsList.IsActive(tab);
		public bool IsActive(ITab tab) => newTabsList.IsActive(tab as Tab);

		public Tab Focused
		{
			get => newTabsList.Focused;
			set => GetUpdateTabsList().Focused = value;
		}
		public ITab FocusedITab => Focused;

		HashSet<Tab> transactionTabs;
		public void AddToTransaction(Tab tab)
		{
			if (transactionTabs.Contains(tab))
				return;
			transactionTabs.Add(tab);
			tab.BeginTransaction(state);
		}


		WindowLayout oldWindowLayout, newWindowLayout;
		public WindowLayout WindowLayout
		{
			get => newWindowLayout;
			set
			{
				EnsureInTransaction();
				newWindowLayout = value;
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

		void BeginTransaction(ExecuteState state)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state;
			transactionTabs = new HashSet<Tab>();
			ActiveTabs.ForEach(AddToTransaction);
		}

		void Rollback()
		{
			EnsureInTransaction();

			newTabsList = oldTabsList;
			newWindowLayout = oldWindowLayout;
			newMacroVisualize = oldMacroVisualize;

			transactionTabs.ForEach(tab => tab.Rollback());
			transactionTabs = null;

			state = null;
		}

		void Commit()
		{
			EnsureInTransaction();

			if (oldTabsList != newTabsList)
			{
				oldTabsList.AllTabs.Null(tab => tab.Tabs).Where(tab => !newTabsList.Contains(tab)).ForEach(tab => tab.Closed());
				var now = DateTime.Now;
				newTabsList.ActiveTabs.ForEach(tab => tab.LastActive = now);
				oldTabsList = newTabsList;
			}
			oldWindowLayout = newWindowLayout;
			oldMacroVisualize = newMacroVisualize;

			transactionTabs.ForEach(tab => tab.Commit());
			transactionTabs = null;

			state = null;
		}
	}
}
