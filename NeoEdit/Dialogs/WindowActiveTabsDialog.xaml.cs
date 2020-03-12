using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class WindowActiveTabsDialog
	{
		[DepProp]
		List<TabsWindow> TabsWindows { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<List<TabsWindow>>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<TextEditor> TextEditors { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<List<TextEditor>>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
		[DepProp]
		bool OneTabSelected { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<bool>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }
		[DepProp]
		TabsWindow MoveToTabsWindow { get { return UIHelper<WindowActiveTabsDialog>.GetPropValue<TabsWindow>(this); } set { UIHelper<WindowActiveTabsDialog>.SetPropValue(this, value); } }

		readonly TabsWindow currentTabWindow;

		List<TabsWindow> SelectedTabsWindows => tabsWindows.SelectedItems.OfType<TabsWindow>().ToList();
		List<TextEditor> SelectedTextEditors => textEditors.SelectedItems.OfType<TextEditor>().ToList();
		TabsWindow SelectedTabsWindow => tabsWindows.SelectedItems.OfType<TabsWindow>().Single();

		static WindowActiveTabsDialog() => UIHelper<WindowActiveTabsDialog>.Register();

		WindowActiveTabsDialog(TabsWindow tabWindow)
		{
			InitializeComponent();
			SetTabsWindows();
			MoveToTabsWindow = currentTabWindow = tabWindow;
		}

		void SetTabsWindows()
		{
			TabsWindows = UIHelper<TabsWindow>.GetAllWindows().OrderByDescending(x => x.LastActivated).ToList();
			if (TabsWindows.Contains(currentTabWindow))
				tabsWindows.SelectedItem = currentTabWindow;
			else
				tabsWindows.SelectedItem = TabsWindows.FirstOrDefault();

			// I don't know why this is necessary, but it refuses to read from a list
			moveToTabsWindow.ItemsSource = UIHelper<TabsWindow>.GetAllWindows().OrderByDescending(x => x.LastActivated).ToDictionary(tabsWindow => tabsWindow.Title, tabsWindow => tabsWindow);
		}

		void OnTabsWindowsSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
		{
			var activeTabs = TabsWindows.SelectMany(tabsWindow => tabsWindow.ActiveTabs).ToList();

			var selectedTabsWindows = SelectedTabsWindows;
			TextEditors = selectedTabsWindows.SelectMany(tabsWindow => tabsWindow.Tabs).ToList();
			OneTabSelected = tabsWindows.SelectedItems.Count == 1;

			textEditors.SelectedItems.Clear();
			foreach (var tab in activeTabs)
				textEditors.SelectedItems.Add(tab);

		}

		void OnTabsWindowNewClick(object sender, RoutedEventArgs e)
		{
			new TabsWindow(true);
			SetTabsWindows();
		}

		void OnTabsWindowCloseClick(object sender, RoutedEventArgs e)
		{
			SelectedTabsWindows.ForEach(tabsWindow => tabsWindow.Close());
			SetTabsWindows();
		}

		List<int> SelectedIndexes(TabsWindow tabsWindow)
		{
			var selected = new HashSet<TextEditor>(textEditors.SelectedItems.OfType<TextEditor>());
			return tabsWindow.Tabs.Indexes(tab => selected.Contains(tab)).ToList();
		}

		void OnTextEditorMoveUpClick(object sender, RoutedEventArgs e)
		{
			var tabsWindow = SelectedTabsWindow;
			var indexes = SelectedIndexes(tabsWindow);
			indexes = indexes.Where((value, index) => value != index).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], indexes[ctr] - 1);
			OnTabsWindowsSelectionChanged();
		}

		void OnTextEditorMoveDownClick(object sender, RoutedEventArgs e)
		{
			var tabsWindow = SelectedTabsWindow;
			var indexes = SelectedIndexes(tabsWindow);
			indexes = indexes.Where((value, index) => value != tabsWindow.Tabs.Count - indexes.Count + index).ToList();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], indexes[ctr] + 1);
			OnTabsWindowsSelectionChanged();
		}

		void OnTextEditorMoveToTopClick(object sender, RoutedEventArgs e)
		{
			var tabsWindow = SelectedTabsWindow;
			var indexes = SelectedIndexes(tabsWindow);
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], ctr);
			OnTabsWindowsSelectionChanged();
		}

		void OnTextEditorMoveToBottomClick(object sender, RoutedEventArgs e)
		{
			var tabsWindow = SelectedTabsWindow;
			var indexes = SelectedIndexes(tabsWindow);
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				tabsWindow.Move(tabsWindow.Tabs[indexes[ctr]], tabsWindow.Tabs.Count - indexes.Count + ctr);
			OnTabsWindowsSelectionChanged();
		}

		void OnTextEditorDiffClick(object sender, RoutedEventArgs e)
		{
			//TODO
			//var textEditors = SelectedTextEditors;
			//if (textEditors.Any(textEditor => textEditor.DiffTarget != null))
			//{
			//	textEditors.ForEach(textEditor => textEditor.DiffTarget = null);
			//	return;
			//}

			//if (textEditors.Count % 2 != 0)
			//	throw new Exception("Must have an even number of tabs selected");
			//for (var ctr = 0; ctr < textEditors.Count; ctr += 2)
			//	textEditors[ctr].DiffTarget = textEditors[ctr + 1];
		}

		void OnTextEditorCloseClick(object sender, RoutedEventArgs e)
		{
			var selectedTextEditors = SelectedTextEditors;

			if (!selectedTextEditors.All(tab => tab.CanClose()))
				return;

			selectedTextEditors.ForEach(item => item.TabsParent.RemoveTextEditor(item));
			OnTabsWindowsSelectionChanged();
		}

		void OnTextEditorMoveClick(object sender, RoutedEventArgs e)
		{
			if (MoveToTabsWindow == null)
				return;

			var selectedTextEditors = SelectedTextEditors;
			selectedTextEditors.ForEach(textEditor => textEditor.TabsParent.RemoveTextEditor(textEditor));
			selectedTextEditors.ForEach(textEditor => MoveToTabsWindow.AddTextEditor(textEditor));
			OnTabsWindowsSelectionChanged();
		}

		public static void Run(TabsWindow tabWindow)
		{
			TabsWindow.ShowIndex = true;
			try { new WindowActiveTabsDialog(tabWindow).ShowDialog(); }
			finally { TabsWindow.ShowIndex = false; }
		}

		void OnTextEditorsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var newActive = SelectedTabsWindows.ToDictionary(tabsWindow => tabsWindow, tabsWindow => new List<TextEditor>());
			foreach (var textEditor in SelectedTextEditors)
				newActive[textEditor.TabsParent].Add(textEditor);
			newActive.ForEach(pair => pair.Key.SetActive(pair.Value));
		}

		void OnOKClick(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
