using System;
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

		public T this[int index]
		{
			get { return list[index]; }
			set
			{
				list[index] = value;
				Notify();
			}
		}

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

		public void Replace(T item)
		{
			list.Clear();
			list.Add(item);
			Notify();
		}

		public void Replace(IEnumerable<T> items)
		{
			list.Clear();
			list.AddRange(items);
			Notify();
		}

		public void Replace(Func<T, T> selector)
		{
			for (var ctr = 0; ctr < list.Count; ++ctr)
				list[ctr] = selector(list[ctr]);
			Notify();
		}

		public bool Remove(T item)
		{
			if (!list.Remove(item))
				return false;
			Notify();
			return true;
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
			Notify();
		}

		public void RemoveDups()
		{
			var changed = false;
			var seen = new HashSet<T>();
			for (var ctr = 0; ctr < list.Count;)
			{
				if (seen.Contains(list[ctr]))
				{
					list.RemoveAt(ctr);
					changed = true;
				}
				else
				{
					seen.Add(list[ctr]);
					++ctr;
				}
			}
			if (changed)
				Notify();
		}

		public int IndexOf(T item)
		{
			return list.IndexOf(item);
		}

		public void InsertAt(int index, T item)
		{
			list.Insert(index, item);
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
