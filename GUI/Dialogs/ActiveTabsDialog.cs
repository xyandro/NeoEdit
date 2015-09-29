using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	class ActiveTabsDialog<ItemType, CommandType> : ModalDialog where ItemType : TabsControl<ItemType, CommandType>
	{
		readonly List<ItemType> originalActive;
		readonly ItemType originalTopMost;
		readonly Tabs<ItemType, CommandType> tabs;
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
			ListView listView;
			var stackPanel = new StackPanel { Margin = new Thickness(10) };
			{
				// Use a copy of Items list because events were still firing after window was closed
				listView = new ListView { ItemsSource = tabs.Items.ToList(), Height = 400, SelectionMode = SelectionMode.Extended };
				{
					var gridView = new GridView();
					gridView.Columns.Add(new GridViewColumn { Header = "Label", DisplayMemberBinding = new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.TabLabel).Name), Width = 500 });
					listView.View = gridView;
				}
				listView.SelectionChanged += (s, e) => SyncItems(listView.SelectedItems.Cast<ItemType>());
				stackPanel.Children.Add(listView);
			}
			{
				var uniformGrid = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Right, Rows = 1, Margin = new Thickness(0, 10, 0, 0) };
				{
					var okButton = new Button { IsDefault = true, Content = "Ok", Padding = new Thickness(10, 1, 10, 1) };
					okButton.Click += (s, e) => DialogResult = true;
					uniformGrid.Children.Add(okButton);
				}
				{
					var cancelButton = new Button { IsCancel = true, Content = "Cancel", Padding = new Thickness(10, 1, 10, 1) };
					cancelButton.Click += (s, e) =>
					{
						SyncItems(originalActive);
						tabs.TopMost = originalTopMost;
						DialogResult = false;
					};
					uniformGrid.Children.Add(cancelButton);
				}
				stackPanel.Children.Add(uniformGrid);
			}
			Content = stackPanel;
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

		public static void Run(Tabs<ItemType, CommandType> tabs)
		{
			new ActiveTabsDialog<ItemType, CommandType>(tabs) { Owner = tabs.WindowParent }.ShowDialog();
		}
	}
}
