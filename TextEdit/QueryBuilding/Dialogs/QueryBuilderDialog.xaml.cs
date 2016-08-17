using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.QueryBuilding.Dialogs
{
	partial class QueryBuilderDialog
	{
		public class GroupByItem
		{
			public string Item { get; set; } = "";
		}

		[DepProp]
		public ObservableCollection<QuerySelect.SelectedData> Selects { get { return UIHelper<QueryBuilderDialog>.GetPropValue<ObservableCollection<QuerySelect.SelectedData>>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<QuerySelect.JoinData> From { get { return UIHelper<QueryBuilderDialog>.GetPropValue<ObservableCollection<QuerySelect.JoinData>>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Where { get { return UIHelper<QueryBuilderDialog>.GetPropValue<string>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<GroupByItem> GroupBy { get { return UIHelper<QueryBuilderDialog>.GetPropValue<ObservableCollection<GroupByItem>>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Having { get { return UIHelper<QueryBuilderDialog>.GetPropValue<string>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<QuerySelect.OrderByData> OrderBy { get { return UIHelper<QueryBuilderDialog>.GetPropValue<ObservableCollection<QuerySelect.OrderByData>>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Count { get { return UIHelper<QueryBuilderDialog>.GetPropValue<int?>(this); } set { UIHelper<QueryBuilderDialog>.SetPropValue(this, value); } }

		public List<QuerySelect.JoinType> JoinTypes { get; } = Helpers.GetValues<QuerySelect.JoinType>().ToList();
		public List<QuerySelect.Directions> Directions { get; } = Helpers.GetValues<QuerySelect.Directions>().ToList();

		bool controlDown => (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None;
		bool altDown => (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None;
		bool shiftDown => (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None;

		static QueryBuilderDialog() { UIHelper<QueryBuilderDialog>.Register(); }

		readonly List<TableSelect> availableTables;
		QueryBuilderDialog(List<TableSelect> availableTables, QuerySelect select)
		{
			this.availableTables = availableTables;
			InitializeComponent();
			Selects = new ObservableCollection<QuerySelect.SelectedData>(select.Selects);
			From = new ObservableCollection<QuerySelect.JoinData>(select.Source);
			Where = select.Where;
			GroupBy = new ObservableCollection<GroupByItem>(select.GroupBy.Select(item => new GroupByItem { Item = item }));
			Having = select.Having;
			OrderBy = new ObservableCollection<QuerySelect.OrderByData>(select.OrderBy);
		}

		List<string> GetColumns() => From.SelectMany(joinData => joinData.Table.Columns.Select(col => $"{joinData.Alias}.{col}")).Distinct().OrderBy().ToList();

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			var focused = Keyboard.FocusedElement as TextBox;
			if (focused == null)
				return;

			if ((e.Key == Key.C) && (shiftDown) && (controlDown))
			{
				e.Handled = true;

				var column = ChooseDialog.Run(Owner, GetColumns());
				if (column == null)
					return;

				TextCompositionManager.StartComposition(new TextComposition(InputManager.Current, focused, column));

				return;
			}

			base.OnPreviewKeyDown(e);
		}

		IEnumerable<DependencyObject> GetVisualChildren(DependencyObject element)
		{
			var count = VisualTreeHelper.GetChildrenCount(element);
			for (var ctr = 0; ctr < count; ++ctr)
			{
				var child = VisualTreeHelper.GetChild(element, ctr);
				yield return child;
				foreach (var item in GetVisualChildren(child))
					yield return item;
			}
		}

		private void AddItem<T>(ListView listView, T item)
		{
			(listView.ItemsSource as IList<T>).Add(item);
			listView.SelectedIndex = listView.Items.Count - 1;
			listView.ScrollIntoView(item);
			listView.UpdateLayout();
			var listViewItem = listView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
			if (listViewItem != null)
			{
				var tb = GetVisualChildren(listViewItem).OfType<TextBox>().FirstOrDefault();
				tb?.SelectAll();
				tb?.Focus();
			}
			listView.SelectedIndex = -1;
		}

		void OnSelectClick(object sender, RoutedEventArgs e) => AddItem(select, new QuerySelect.SelectedData());

		void DeleteSelect(object sender, RoutedEventArgs e) => Selects.Remove((sender as Button).Tag as QuerySelect.SelectedData);

		void OnFromClick(object sender, RoutedEventArgs e)
		{
			var table = ChooseDialog.Run(Owner, availableTables, val => val.Table);
			if (table == null)
				return;

			AddItem(from, new QuerySelect.JoinData { Type = From.Any() ? QuerySelect.JoinType.InnerJoin : QuerySelect.JoinType.Normal, Table = table, Alias = table.Table });
		}

		void DeleteFrom(object sender, RoutedEventArgs e) => From.Remove((sender as Button).Tag as QuerySelect.JoinData);

		void OnWhereClick(object sender, RoutedEventArgs e) => where.Focus();

		void OnGroupByClick(object sender, RoutedEventArgs e) => AddItem(groupBy, new GroupByItem());

		void DeleteGroupBy(object sender, RoutedEventArgs e) => GroupBy.Remove((sender as Button).Tag as GroupByItem);

		void OnHavingClick(object sender, RoutedEventArgs e) => having.Focus();

		void OnOrderByClick(object sender, RoutedEventArgs e) => AddItem(orderBy, new QuerySelect.OrderByData());

		void DeleteOrderBy(object sender, RoutedEventArgs e) => OrderBy.Remove((sender as Button).Tag as QuerySelect.OrderByData);

		void OnCountClick(object sender, RoutedEventArgs e) => count.Focus();

		QuerySelect result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			result = new QuerySelect
			{
				Selects = new List<QuerySelect.SelectedData>(Selects),
				Source = new List<QuerySelect.JoinData>(From),
				Where = Where,
				GroupBy = new List<string>(GroupBy.Select(item => item.Item)),
				Having = Having,
				OrderBy = new List<QuerySelect.OrderByData>(OrderBy),
			};
		}

		public static QuerySelect Run(Window parent, List<TableSelect> availableTables, QuerySelect query)
		{
			var dialog = new QueryBuilderDialog(availableTables, query) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}
