using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace NeoEdit.GUI.Controls
{
	class ActiveTabsDialog<ItemType> : ModalDialog where ItemType : FrameworkElement
	{
		readonly List<Tabs<ItemType>.ItemData> original;
		readonly ObservableCollection<Tabs<ItemType>.ItemData> items;
		public ActiveTabsDialog(ObservableCollection<Tabs<ItemType>.ItemData> items, string tabLabelPath)
		{
			this.items = items;
			original = items.Where(item => item.Active).ToList();
			Setup(tabLabelPath);
		}

		void Setup(string tabLabelPath)
		{
			Title = "Active Tabs";
			ListView listView;
			var stackPanel = new StackPanel { Margin = new Thickness(10) };
			{
				listView = new ListView { ItemsSource = items, Height = 400, SelectionMode = SelectionMode.Extended };
				{
					var gridView = new GridView();
					gridView.Columns.Add(new GridViewColumn { Header = "Label", DisplayMemberBinding = new Binding(UIHelper<Tabs<ItemType>.ItemData>.GetProperty(a => a.Item).Name) { Converter = new GetLabelConverter(tabLabelPath) }, Width = 500 });
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
						SyncItems(original);
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
			foreach (var item in original)
				listView.SelectedItems.Add(item);
		}

		void SyncItems(IEnumerable<Tabs<ItemType>.ItemData> active)
		{
			var activeHash = new HashSet<Tabs<ItemType>.ItemData>(active);
			foreach (var item in items)
				item.Active = activeHash.Contains(item);
		}

		public static void Run(Window parent, ObservableCollection<Tabs<ItemType>.ItemData> items, string tabLabelPath)
		{
			new ActiveTabsDialog<ItemType>(items, tabLabelPath) { Owner = parent }.ShowDialog();
		}

		public class GetLabelConverter : IValueConverter
		{
			readonly string tabLabelPath;
			public GetLabelConverter(string tabLabelPath)
			{
				this.tabLabelPath = tabLabelPath;
			}

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (value == null)
					return null;

				return value.GetType().GetProperty(tabLabelPath).GetValue(value);
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}

		}
	}
}
