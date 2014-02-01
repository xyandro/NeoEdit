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
			get { return GetProperty<string>(RecordProperty.PropertyName.FullName); }
			protected set { SetProperty(RecordProperty.PropertyName.FullName, value); }
		}

		public IEnumerable<RecordProperty.PropertyName> Properties
		{
			get { return dependencyProperty.Where(a => GetValue(a.Value) != null).Select(a => a.Key); }
		}

		public virtual IEnumerable<RecordAction.ActionName> Actions
		{
			get { return new List<RecordAction.ActionName> { RecordAction.ActionName.Sync }; }
		}

		protected T GetProperty<T>(RecordProperty.PropertyName property)
		{
			return (T)GetValue(dependencyProperty[property]);
		}

		protected virtual void SetProperty<T>(RecordProperty.PropertyName property, T value)
		{
			SetValue(dependencyProperty[property], value);
			switch (property)
			{
				case RecordProperty.PropertyName.FullName:
					this[RecordProperty.PropertyName.Path] = Path.GetDirectoryName(FullName);
					this[RecordProperty.PropertyName.Name] = Path.GetFileName(FullName);
					break;
				case RecordProperty.PropertyName.Name:
					this[RecordProperty.PropertyName.NameWoExtension] = value;
					this[RecordProperty.PropertyName.Extension] = "";
					break;
			}
		}

		public object this[RecordProperty.PropertyName property]
		{
			get { return GetProperty<object>(property); }
			protected set { SetProperty(property, value); }
		}

		public virtual string Name
		{
			get { return GetProperty<string>(RecordProperty.PropertyName.Name); }
		}

		public virtual bool IsFile { get { return false; } }

		protected virtual IEnumerable<Record> InternalRecords { get { return new List<Record>(); } }
		readonly ObservableCollection<Record> records = new ObservableCollection<Record>();
		public ObservableCollection<Record> Records { get { Refresh(); return records; } }

		public void RemoveChild(string childFullName)
		{
			var child = records.SingleOrDefault(a => a.FullName == childFullName);
			if (child != null)
				RemoveChild(child);
		}

		public void RemoveFromParent()
		{
			Parent.RemoveChild(this);
		}

		public virtual void RemoveChild(Record record)
		{
			records.Remove(record);
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
		public virtual void Delete() { }
		public virtual void Paste() { }
		public virtual void CalcMD5() { }
		public virtual void Identify() { }
		public virtual void Sync(Record source) { }

		public override string ToString()
		{
			return FullName;
		}
	}
}
