using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.Common;

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

		protected Record(string uri)
		{
			FullName = uri;
		}
		public virtual Record Parent { get { return new Root(); } }
		public virtual string FullName
		{
			get { return GetProperty<string>(RecordProperty.PropertyName.FullName); }
			protected set { SetProperty(RecordProperty.PropertyName.FullName, value); }
		}
		public virtual string Name { get { return GetProperty<string>(RecordProperty.PropertyName.Name); } }
		public virtual bool IsFile { get { return false; } }

		public IEnumerable<RecordProperty.PropertyName> Properties { get { return dependencyProperty.Where(a => GetValue(a.Value) != null).Select(a => a.Key); } }
		public virtual IEnumerable<RecordAction.ActionName> Actions { get { return new List<RecordAction.ActionName> { RecordAction.ActionName.Sync }; } }
		protected T GetProperty<T>(RecordProperty.PropertyName property) { return (T)GetValue(dependencyProperty[property]); }

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

		public virtual IEnumerable<Record> Records { get { return null; } }

		public virtual void Rename(string newName, Func<bool> canOverwrite) { }
		public virtual void Delete() { }
		public virtual void Paste() { }
		public virtual void CalcMD5() { }
		public virtual void Identify() { }
		public virtual void Sync(Record source) { }
		public virtual BinaryData Read() { throw new Exception("Cannot read file"); }

		public virtual Type GetRootType() { return typeof(Record); }

		public virtual Record GetRecord(string uri)
		{
			string findUri = "", remaining = uri;
			var record = this as Record;
			while (record != null)
			{
				if (uri.Equals(record.FullName, StringComparison.OrdinalIgnoreCase))
					return record;

				var match = new Regex(@"^(\\*[^\\]+)(.*)$").Match(remaining);
				if (!match.Success)
					break;
				findUri += match.Groups[1].Value;
				remaining = match.Groups[2].Value;

				record = record.Records.SingleOrDefault(a => a.FullName.Equals(findUri, StringComparison.OrdinalIgnoreCase));
			}

			throw new Exception(String.Format("Invalid input: {0}", uri));
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}
