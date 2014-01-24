using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		protected Record(string uri, Record parent)
		{
			this[Property.PropertyType.FullName] = uri;
			this[Property.PropertyType.Name] = Path.GetFileName(FullName);
			this[Property.PropertyType.Path] = Path.GetDirectoryName(FullName);
			this[Property.PropertyType.Extension] = Path.GetExtension(FullName);

			Parent = parent == null ? this : parent;
		}
		public Record Parent { get; private set; }
		public string FullName { get { return Prop<string>(Property.PropertyType.FullName); } }

		Dictionary<Property.PropertyType, object> properties = new Dictionary<Property.PropertyType, object>();

		public IEnumerable<Property.PropertyType> Properties { get { return properties.Keys; } }

		public T Prop<T>(Property.PropertyType property)
		{
			return (T)this[property];
		}

		public object this[Property.PropertyType property]
		{
			get
			{
				if (properties.ContainsKey(property))
					return properties[property];

				return null;
			}
			protected set { properties[property] = value; }
		}

		public virtual string Name
		{
			get { return Prop<string>(Property.PropertyType.Name); }
		}

		public virtual bool IsFile { get { return false; } }

		protected virtual IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords { get { return new List<Tuple<string, Func<string, Record>>>(); } }
		readonly ObservableCollection<Record> records = new ObservableCollection<Record>();
		public ObservableCollection<Record> Records { get { Refresh(); return records; } }

		public void Refresh()
		{
			var existingList = records.ToDictionary(a => a.FullName, a => a);
			var newList = InternalRecords.ToDictionary(a => a.Item1, a => a);

			var toAdd = newList.Where(a => !existingList.Keys.Contains(a.Key));
			var toRemove = existingList.Where(a => !newList.Keys.Contains(a.Key));

			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				foreach (var add in toAdd)
					records.Add(add.Value.Item2(add.Value.Item1));

				foreach (var remove in toRemove)
					records.Remove(remove.Value);
			})).Wait();
		}
	}
}
