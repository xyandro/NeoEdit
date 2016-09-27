using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	class ActiveTabsDialog<ItemType, CommandType> : ModalDialog where ItemType : TabsControl<ItemType, CommandType>
	{
		readonly List<ItemType> originalActive;
		readonly ItemType originalTopMost;
		readonly Tabs<ItemType, CommandType> tabs;
		ListView listView;

		public ActiveTabsDialog(Tabs<ItemType, CommandType> tabs)
		{
			this.tabs = tabs;
			originalActive = tabs.Items.Where(item => item.Active).ToList();
			originalTopMost = tabs.TopMost;
			Setup();
		}

		void Setup()
		{
			Title = "Active Tabs";
			var grid = new Grid { Margin = new Thickness(10) };

			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			// Use a copy of Items list because events were still firing after window was closed
			listView = new ListView { ItemsSource = tabs.Items.ToList(), Height = 400, SelectionMode = SelectionMode.Extended };
			{
				var gridView = new GridView();
				gridView.Columns.Add(new GridViewColumn { Header = "Label", DisplayMemberBinding = new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.TabLabel).Name), Width = 500 });
				listView.View = gridView;
			}
			listView.SelectionChanged += (s, e) => SyncItems(listView.SelectedItems.Cast<ItemType>());
			Grid.SetRow(listView, 0);
			Grid.SetColumn(listView, 0);
			grid.Children.Add(listView);

			var stackPanel = new StackPanel();
			var moveUp = new Button { Content = "Move _Up" };
			moveUp.Click += MoveUp_Click;
			stackPanel.Children.Add(moveUp);
			var moveDown = new Button { Content = "Move _Down" };
			moveDown.Click += MoveDown_Click;
			stackPanel.Children.Add(moveDown);
			var moveToTop = new Button { Content = "Move To _Top", Margin = new Thickness(0, 10, 0, 0) };
			moveToTop.Click += MoveToTop_Click;
			stackPanel.Children.Add(moveToTop);
			var moveToBottom = new Button { Content = "Move To _Bottom" };
			moveToBottom.Click += MoveToBottom_Click;
			stackPanel.Children.Add(moveToBottom);
			Grid.SetRow(stackPanel, 0);
			Grid.SetColumn(stackPanel, 1);
			grid.Children.Add(stackPanel);

			var uniformGrid = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Right, Rows = 1, Margin = new Thickness(0, 10, 0, 0) };

			var okButton = new Button { IsDefault = true, Content = "Ok", Padding = new Thickness(10, 1, 10, 1) };
			okButton.Click += (s, e) => DialogResult = true;
			uniformGrid.Children.Add(okButton);

			var cancelButton = new Button { IsCancel = true, Content = "Cancel", Padding = new Thickness(10, 1, 10, 1) };
			cancelButton.Click += (s, e) =>
			{
				SyncItems(originalActive);
				tabs.TopMost = originalTopMost;
				DialogResult = false;
			};
			uniformGrid.Children.Add(cancelButton);

			Grid.SetRow(uniformGrid, 2);
			Grid.SetColumn(uniformGrid, 0);
			Grid.SetColumnSpan(uniformGrid, 2);
			grid.Children.Add(uniformGrid);

			Content = grid;
			listView.Focus();
			SizeToContent = SizeToContent.WidthAndHeight;

			listView.SelectedItems.Clear();
			foreach (var item in originalActive)
				listView.SelectedItems.Add(item);

			listView.Loaded += (s, e) =>
			{
				var focusItem = originalActive.FirstOrDefault();
				if (focusItem != null)
				{
					listView.ScrollIntoView(focusItem);
					var item = listView.ItemContainerGenerator.ContainerFromItem(focusItem) as ListBoxItem;
					if (item != null)
						item.Focus();
				}
			};
		}

		List<int> SelectedIndexes()
		{
			var selected = new HashSet<ItemType>(listView.SelectedItems.Cast<ItemType>());
			return tabs.Items.Indexes(tab => selected.Contains(tab)).ToList();
		}

		void MoveUp_Click(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != index).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				tabs.Items.Move(indexes[ctr], indexes[ctr] - 1);
			listView.ItemsSource = tabs.Items.ToList();
		}

		void MoveDown_Click(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != tabs.Items.Count - indexes.Count + index).ToList();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				tabs.Items.Move(indexes[ctr], indexes[ctr] + 1);
			listView.ItemsSource = tabs.Items.ToList();
		}

		void MoveToTop_Click(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				tabs.Items.Move(indexes[ctr], ctr);
			listView.ItemsSource = tabs.Items.ToList();
		}

		void MoveToBottom_Click(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				tabs.Items.Move(indexes[ctr], tabs.Items.Count - indexes.Count + ctr);
			listView.ItemsSource = tabs.Items.ToList();
		}

		void SyncItems(IEnumerable<ItemType> active)
		{
			var activeHash = new HashSet<ItemType>(active);
			foreach (var item in tabs.Items)
				item.Active = activeHash.Contains(item);
			var topMost = tabs.TopMost;
			if (!activeHash.Contains(topMost))
				topMost = active.FirstOrDefault();
			if (topMost != null)
				tabs.TopMost = topMost;
		}

		public static void Run(Tabs<ItemType, CommandType> tabs) => new ActiveTabsDialog<ItemType, CommandType>(tabs) { Owner = tabs.WindowParent }.ShowDialog();
	}
}
