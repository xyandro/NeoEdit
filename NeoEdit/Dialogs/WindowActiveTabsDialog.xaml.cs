//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows;
//using System.Windows.Controls;
//using NeoEdit.Program;
//using NeoEdit.Program.Controls;

//namespace NeoEdit.Program.Dialogs
//{
//	partial class WindowActiveTabsDialog
//	{
//		[DepProp]
//		List<TabsWindow> TabsWindows { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<List<TabsWindow>>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
//		[DepProp]
//		List<TabData> Tabs { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<List<TabData>>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
//		[DepProp]
//		bool OneTabSelected { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<bool>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
//		[DepProp]
//		TabsWindow MoveToTabsWindow { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<TabsWindow>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }

//		readonly TabsWindow currentTabWindow;

//		List<TabsWindow> SelectedTabsWindows => tabsWindows.SelectedItems.OfType<TabsWindow>().ToList();
//		List<TabData> SelectedTabs => tabs.SelectedItems.OfType<TabData>().ToList();
//		TabsWindow SelectedTabsWindow => tabsWindows.SelectedItems.OfType<TabsWindow>().Single();

//		static WindowActiveTabsDialog() => UIHelper<WindowActiveTabsDialog>.Register();

//		WindowActiveTabsDialog(TabsWindow tabWindow)
//		{
//			InitializeComponent();
//			SetTabsWindows();
//			MoveToTabsWindow = currentTabWindow = tabWindow;
//		}

//		void SetTabsWindows()
//		{
//			TabsWindows = UIHelper<TabsWindow>.GetAllWindows().OrderByDescending(x => x.LastActivated).ToList();
//			if (TabsWindows.Contains(currentTabWindow))
//				tabsWindows.SelectedItem = currentTabWindow;
//			else
//				tabsWindows.SelectedItem = TabsWindows.FirstOrDefault();

//			// I don't know why this is necessary, but it refuses to read from a list
//			moveToTabsWindow.ItemsSource = UIHelper<TabsWindow>.GetAllWindows().OrderByDescending(x => x.LastActivated).ToDictionary(tabsWindow => tabsWindow.Title, tabsWindow => tabsWindow);
//		}

//		void OnTabsWindowsSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
//		{
//			var activeTabs = TabsWindows.SelectMany(tabsWindow => tabsWindow.ActiveTabs).ToList();

//			var selectedTabsWindows = SelectedTabsWindows;
//			Tabs = selectedTabsWindows.SelectMany(tabsWindow => tabsWindow.Tabs).ToList();
//			OneTabSelected = tabsWindows.SelectedItems.Count == 1;

//			tabs.SelectedItems.Clear();
//			foreach (var tab in activeTabs)
//				tabs.SelectedItems.Add(tab);

//		}

//		void OnTabsWindowNewClick(object sender, RoutedEventArgs e)
//		{
//			new TabsWindow(true);
//			SetTabsWindows();
//		}

//		void OnTabsWindowCloseClick(object sender, RoutedEventArgs e)
//		{
//			SelectedTabsWindows.ForEach(tabsWindow => tabsWindow.Close());
//			SetTabsWindows();
//		}

//		List<int> SelectedIndexes(TabsWindow tabsWindow)
//		{
//			var selected = new HashSet<TabData>(tabs.SelectedItems.OfType<TabData>());
//			return tabsWindow.Tabs.Indexes(tab => selected.Contains(tab)).ToList();
//		}

//		void OnTabMoveUpClick(object sender, RoutedEventArgs e)
//		{
//			var tabsWindow = SelectedTabsWindow;
//			var indexes = SelectedIndexes(tabsWindow);
//			indexes = indexes.Where((value, index) => value != index).ToList();
//			for (var ctr = 0; ctr < indexes.Count; ++ctr)
//				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], indexes[ctr] - 1);
//			OnTabsWindowsSelectionChanged();
//		}

//		void OnTabMoveDownClick(object sender, RoutedEventArgs e)
//		{
//			var tabsWindow = SelectedTabsWindow;
//			var indexes = SelectedIndexes(tabsWindow);
//			indexes = indexes.Where((value, index) => value != tabsWindow.Tabs.Count - indexes.Count + index).ToList();
//			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
//				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], indexes[ctr] + 1);
//			OnTabsWindowsSelectionChanged();
//		}

//		void OnTabMoveToTopClick(object sender, RoutedEventArgs e)
//		{
//			var tabsWindow = SelectedTabsWindow;
//			var indexes = SelectedIndexes(tabsWindow);
//			for (var ctr = 0; ctr < indexes.Count; ++ctr)
//				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], ctr);
//			OnTabsWindowsSelectionChanged();
//		}

//		void OnTabMoveToBottomClick(object sender, RoutedEventArgs e)
//		{
//			var tabsWindow = SelectedTabsWindow;
//			var indexes = SelectedIndexes(tabsWindow);
//			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
//				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], tabsWindow.Tabs.Count - indexes.Count + ctr);
//			OnTabsWindowsSelectionChanged();
//		}

//		void OnTabDiffClick(object sender, RoutedEventArgs e)
//		{
//			//TODO
//			//var tabs = SelectedTabs;
//			//if (tabs.Any(tab => tab.DiffTarget != null))
//			//{
//			//	tabs.ForEach(tab => tab.DiffTarget = null);
//			//	return;
//			//}

//			//if (tabs.Count % 2 != 0)
//			//	throw new Exception("Must have an even number of tabs selected");
//			//for (var ctr = 0; ctr < tabs.Count; ctr += 2)
//			//	tabs[ctr].DiffTarget = tabs[ctr + 1];
//		}

//		void OnTabCloseClick(object sender, RoutedEventArgs e)
//		{
//			var selectedTabs = SelectedTabs;

//			if (!selectedTabs.All(tab => tab.CanClose()))
//				return;

//			selectedTabs.ForEach(item => item.TabsParent.RemoveTab(item));
//			OnTabsWindowsSelectionChanged();
//		}

//		void OnTabMoveClick(object sender, RoutedEventArgs e)
//		{
//			if (MoveToTabsWindow == null)
//				return;

//			var selectedTabs = SelectedTabs;
//			selectedTabs.ForEach(tab => tab.TabsParent.RemoveTab(tab));
//			selectedTabs.ForEach(tab => MoveToTabsWindow.AddTab(tab));
//			OnTabsWindowsSelectionChanged();
//		}

//		public static void Run(TabsWindow tabWindow)
//		{
//			TabsWindow.ShowIndex = true;
//			try { new WindowActiveTabsDialog(tabWindow).ShowDialog(); }
//			finally { TabsWindow.ShowIndex = false; }
//		}

//		void OnTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
//		{
//			var newActive = SelectedTabsWindows.ToDictionary(tabsWindow => tabsWindow, tabsWindow => new List<TabData>());
//			foreach (var tab in SelectedTabs)
//				newActive[tab.TabsParent].Add(tab);
//			newActive.ForEach(pair => pair.Key.SetActive(pair.Value));
//		}

//		void OnOKClick(object sender, RoutedEventArgs e) => DialogResult = true;
//	}
//}
