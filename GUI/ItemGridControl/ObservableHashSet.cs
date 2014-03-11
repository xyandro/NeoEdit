using System.Collections.ObjectModel;

namespace NeoEdit.GUI.ItemGridControl
{
	public class ObservableHashSet<T> : ObservableCollection<T>
	{
		protected override void InsertItem(int index, T item)
		{
			if (Contains(item))
				return;
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item)
		{
			int i = IndexOf(item);
			if ((i >= 0) && (i != index))
				return;
			base.SetItem(index, item);
		}
	}
}
