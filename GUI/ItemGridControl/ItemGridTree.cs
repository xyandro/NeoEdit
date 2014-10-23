using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.ItemGridControl
{
	public class ItemGridTree : ItemGrid
	{
		public static DependencyProperty LocationProperty = DependencyProperty.Register("Location", typeof(ItemGridTreeItem), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGridTree).OnLocationChanged(e.OldValue as ItemGridTreeItem)));

		public ItemGridTreeItem Location { get { return (ItemGridTreeItem)GetValue(LocationProperty); } set { SetValue(LocationProperty, value); } }

		Stack<ItemGridTreeItem> lastLocation = new Stack<ItemGridTreeItem>();
		Stack<ItemGridTreeItem> nextLocation = new Stack<ItemGridTreeItem>();

		public ItemGridTree()
		{
			Accept += ItemGridTree_Accept;
		}

		protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Handled)
				return;

			var keySet = new KeySet
			{
				{ ModifierKeys.Alt, Key.Up, () => {
					var parent = Location.GetParent();
					if (parent != null)
						Location = parent;
				}},
				{ ModifierKeys.Alt, Key.Left, () => SetLastLocation() },
				{ ModifierKeys.Alt, Key.Right, () => SetNextLocation() },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		bool isInternal = false;
		void SetLastLocation()
		{
			if (!lastLocation.Any())
				return;

			isInternal = true;
			nextLocation.Push(Location);
			Location = lastLocation.Pop();
			isInternal = false;
		}

		void SetNextLocation()
		{
			if (!nextLocation.Any())
				return;

			isInternal = true;
			lastLocation.Push(Location);
			Location = nextLocation.Pop();
			isInternal = false;
		}

		void ItemGridTree_Accept(object sender, RoutedEventArgs e)
		{
			if (Selected.Count != 1)
				return;
			if (!(Selected[0] as ItemGridTreeItem).CanGetChildren())
				return;
			Location = Selected[0] as ItemGridTreeItem;
		}

		void OnLocationChanged(ItemGridTreeItem last)
		{
			if ((!isInternal) && (last != null))
			{
				lastLocation.Push(last);
				nextLocation.Clear();
			}

			if (!Location.CanGetChildren())
			{
				isInternal = true;
				Location = Location.GetParent();
				isInternal = false;
				return;
			}

			var oldLocation = last == null ? null : last.FullName;
			Items = new ObservableCollection<ItemGridTreeItem>(Location.GetChildren());
			ResetScroll();
			if (oldLocation == null)
				return;
			Focused = Items.FirstOrDefault(item => (item as ItemGridTreeItem).FullName == oldLocation);
			if (Focused == null)
				return;
			Selected.Add(Focused);
		}

		public void Refresh()
		{
			SyncItems(Location.GetChildren(), UIHelper<ItemGridTreeItem>.GetProperty(a => a.FullName));
		}
	}
}
