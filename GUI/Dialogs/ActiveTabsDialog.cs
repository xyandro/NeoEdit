using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	class ActiveTabsDialog<ItemType> : ModalDialog where ItemType : FrameworkElement
	{
		readonly List<Tabs<ItemType>.ItemData> originalActive;
		readonly Tabs<ItemType>.ItemData originalTopMost;
		readonly Tabs<ItemType> tabs;
		public ActiveTabsDialog(Tabs<ItemType> tabs)
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
					gridView.Columns.Add(new GridViewColumn { Header = "Label", DisplayMemberBinding = tabs.TabLabelBinding, Width = 500 });
					listView.View = gridView;
				}
				listView.SelectionChanged += (s, e) => SyncItems(listView.SelectedItems.Cast<Tabs<ItemType>.ItemData>());
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

		void SyncItems(IEnumerable<Tabs<ItemType>.ItemData> active)
		{
			var activeHash = new HashSet<Tabs<ItemType>.ItemData>(active);
			foreach (var item in tabs.Items)
				item.Active = activeHash.Contains(item);
			var topMost = tabs.TopMost;
			if (!activeHash.Contains(topMost))
				topMost = active.FirstOrDefault();
			if (topMost != null)
				tabs.TopMost = topMost;
		}

		public static void Run(Tabs<ItemType> tabs)
		{
			new ActiveTabsDialog<ItemType>(tabs) { Owner = UIHelper.FindParent<Window>(tabs) }.ShowDialog();
		}
	}
}
