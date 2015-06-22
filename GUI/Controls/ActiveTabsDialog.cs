using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace NeoEdit.GUI.Controls
{
	class ActiveTabsDialog<ItemType> : ModalDialog where ItemType : FrameworkElement
	{
		public ActiveTabsDialog(ObservableCollection<Tabs<ItemType>.ItemData> items, string tabLabelPath)
		{
			Setup(items, tabLabelPath);
		}

		void Setup(ObservableCollection<Tabs<ItemType>.ItemData> items, string tabLabelPath)
		{
			Title = "Active Tabs";
			ListView listView;
			var stackPanel = new StackPanel { Margin = new Thickness(10) };
			{
				listView = new ListView { ItemsSource = items, Height = 400 };
				{
					var gridView = new GridView();
					{
						var column = new GridViewColumn { Header = "Active" };
						{
							var dataTemplate = new DataTemplate();
							{
								var checkBox = new FrameworkElementFactory(typeof(CheckBox));
								checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(UIHelper<Tabs<ItemType>.ItemData>.GetProperty(a => a.Active).Name));
								dataTemplate.VisualTree = checkBox;
							}
							column.CellTemplate = dataTemplate;
						}
						gridView.Columns.Add(column);
					}
					gridView.Columns.Add(new GridViewColumn { Header = "Label", DisplayMemberBinding = new Binding(UIHelper<Tabs<ItemType>.ItemData>.GetProperty(a => a.Item).Name) { Converter = new GetLabelConverter(tabLabelPath) }, Width = 500 });
					listView.View = gridView;
				}
				listView.PreviewKeyDown += (s, e) =>
				{
					if (e.Key == Key.Space)
					{
						var selected = listView.SelectedItems.Cast<Tabs<ItemType>.ItemData>().ToList();
						var status = !selected.All(item => item.Active == true);
						selected.ForEach(item => item.Active = status);
						e.Handled = true;
					}
				};
				stackPanel.Children.Add(listView);
			}
			{
				var uniformGrid = new UniformGrid { HorizontalAlignment = HorizontalAlignment.Right, Rows = 1, Margin = new Thickness(0, 10, 0, 0) };
				{
					var okButton = new Button { IsDefault = true, IsCancel = true, Content = "Ok", Padding = new Thickness(10, 1, 10, 1) };
					okButton.Click += (s, e) => DialogResult = true;
					uniformGrid.Children.Add(okButton);
				}
				stackPanel.Children.Add(uniformGrid);
			}
			Content = stackPanel;
			listView.Focus();
			SizeToContent = SizeToContent.WidthAndHeight;
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
