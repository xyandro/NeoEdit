using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class WindowActiveTabsDialog : NEWindow
	{
		readonly WindowActiveTabsDialogData data;

		static WindowActiveTabsDialog()
		{
			UIHelper<WindowActiveTabsDialog>.Register();
		}

		WindowActiveTabsDialog(WindowActiveTabsDialogData data)
		{
			this.data = data;
			InitializeComponent();
			SyncData();
		}

		void SyncData()
		{
			var newdata = data.AllTabs.Select((str, index) => Tuple.Create(str, index)).ToList();

			tabs.SelectionChanged -= OnTabsSelectionChanged;
			tabs.UnselectAll();
			tabs.ItemsSource = newdata;
			data.ActiveIndexes.ForEach(index => tabs.SelectedItems.Add(newdata[index]));
			data.FocusedIndex = data.FocusedIndex;
			tabs.SelectionChanged += OnTabsSelectionChanged;
		}

		List<int> SelectedIndexes() => tabs.SelectedItems.OfType<Tuple<string, int>>().Select(pair => pair.Item2).ToList();

		void OnTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			data.SetActiveIndexes(SelectedIndexes());
			SyncData();
		}

		void OnTabMoveUpClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != index).ToList();
			var moves = new List<(int, int)>();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				moves.Add((indexes[ctr], indexes[ctr] - 1));
			data.DoMoves(moves);
			SyncData();
		}

		void OnTabMoveDownClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != data.AllTabs.Count - indexes.Count + index).ToList();
			var moves = new List<(int, int)>();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				moves.Add((indexes[ctr], indexes[ctr] + 1));
			data.DoMoves(moves);
			SyncData();
		}

		void OnTabMoveToTopClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			var moves = new List<(int, int)>();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				moves.Add((indexes[ctr], ctr));
			data.DoMoves(moves);
			SyncData();
		}

		void OnTabMoveToBottomClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			var moves = new List<(int, int)>();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				moves.Add((indexes[ctr], data.AllTabs.Count - indexes.Count + ctr));
			data.DoMoves(moves);
			SyncData();
		}

		void OnTabCloseClick(object sender, RoutedEventArgs e)
		{
			data.CloseTabs(SelectedIndexes());
			SyncData();
		}

		void OnOKClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static void Run(Window window, WindowActiveTabsDialogData data)
		{
			if (!new WindowActiveTabsDialog(data) { Owner = window }.ShowDialog())
				throw new OperationCanceledException();
		}
	}
}
