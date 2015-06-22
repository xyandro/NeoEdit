using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NeoEdit.Common
{
	public class ObservableCollectionEx<T> : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;

		readonly List<T> list;
		public ObservableCollectionEx()
		{
			list = new List<T>();
		}

		public ObservableCollectionEx(IEnumerable<T> collection)
		{
			list = new List<T>(collection);
		}

		public ObservableCollectionEx(List<T> list)
		{
			list = new List<T>(list);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count { get { return list.Count; } }

		public T this[int index] { get { return list[index]; } }

		public void Clear()
		{
			list.Clear();
			Notify();
		}

		public void Add(T item)
		{
			list.Add(item);
			Notify();
		}

		public void AddRange(IEnumerable<T> items)
		{
			list.AddRange(items);
			Notify();
		}

		void Notify()
		{
			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs("Count"));
		}
	}
}
