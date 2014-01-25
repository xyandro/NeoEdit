using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace NeoEdit.Records
{
	public abstract class Record : DependencyObject
	{
		static Dictionary<RecordProperty.PropertyName, DependencyProperty> dependencyProperty;
		static Record()
		{
			var properties = Enum.GetValues(typeof(RecordProperty.PropertyName)).Cast<RecordProperty.PropertyName>().ToList();
			dependencyProperty = properties.ToDictionary(a => a, a => DependencyProperty.Register(a.ToString(), RecordProperty.Get(a).Type, typeof(Record)));
		}

		protected Record(string uri, Record parent)
		{
			FullName = uri;
			Parent = parent == null ? this : parent;
		}
		public Record Parent { get; private set; }
		public virtual string FullName
		{
			get { return Prop<string>(RecordProperty.PropertyName.FullName); }
			protected set
			{
				this[RecordProperty.PropertyName.FullName] = value;
				this[RecordProperty.PropertyName.Name] = Path.GetFileName(FullName);
				this[RecordProperty.PropertyName.Path] = Path.GetDirectoryName(FullName);
			}
		}

		public IEnumerable<RecordProperty.PropertyName> Properties
		{
			get { return dependencyProperty.Where(a => GetValue(a.Value) != null).Select(a => a.Key); }
		}

		public virtual IEnumerable<RecordAction.ActionName> Actions
		{
			get { return new List<RecordAction.ActionName>(); }
		}

		public T Prop<T>(RecordProperty.PropertyName property)
		{
			return (T)this[property];
		}

		public object this[RecordProperty.PropertyName property]
		{
			get { return GetValue(dependencyProperty[property]); }
			protected set { SetValue(dependencyProperty[property], value); }
		}

		public virtual string Name
		{
			get { return Prop<string>(RecordProperty.PropertyName.Name); }
		}

		public virtual bool IsFile { get { return false; } }

		protected virtual IEnumerable<Record> InternalRecords { get { return new List<Record>(); } }
		readonly ObservableCollection<Record> records = new ObservableCollection<Record>();
		public ObservableCollection<Record> Records { get { Refresh(); return records; } }

		public virtual void RemoveChild(string childFullName)
		{
			var child = records.SingleOrDefault(a => a.FullName == childFullName);
			if (child != null)
				records.Remove(child);
		}

		public void Refresh()
		{
			var existingList = records.ToDictionary(a => a.FullName, a => a);
			var newList = InternalRecords.ToDictionary(a => a.FullName, a => a);

			var toAdd = newList.Where(a => !existingList.Keys.Contains(a.Key));
			var toRemove = existingList.Where(a => !newList.Keys.Contains(a.Key));

			foreach (var add in toAdd)
				records.Add(add.Value);

			foreach (var remove in toRemove)
				records.Remove(remove.Value);
		}

		public virtual void Rename(string newName, Func<bool> canOverwrite) { }

		public override string ToString()
		{
			return FullName;
		}
	}
}
