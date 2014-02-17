using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
					{
						var idx = FullName.LastIndexOf('\\');
						if (idx == -1)
						{
							this[RecordProperty.PropertyName.Path] = "";
							this[RecordProperty.PropertyName.Name] = FullName;
						}
						else
						{
							this[RecordProperty.PropertyName.Path] = FullName.Substring(0, idx);
							this[RecordProperty.PropertyName.Name] = FullName.Substring(idx + 1);
						}
					}
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

		public virtual Record CreateFile(string name) { throw new NotImplementedException(); }
		public virtual Record CreateDirectory(string name) { throw new NotImplementedException(); }

		public virtual void MoveFrom(Record source, string newName = null)
		{
			if (newName == null)
				newName = source.Name;

			if (source.IsFile)
			{
				var data = source.Read();

				var newFile = CreateFile(newName);
				newFile.Write(data);

				source.Delete();
				return;
			}

			var dir = CreateDirectory(newName);
			foreach (var record in source.Records)
				dir.MoveFrom(record);
			source.Delete();
		}

		public virtual void Delete() { }
		public virtual void CalcMD5() { }
		public virtual void Identify() { }

		public void CopyFrom(Record source, string newName = null)
		{
			Record dest;
			if (source.IsFile)
				dest = CreateFile(newName ?? source.Name);
			else
				dest = CreateDirectory(newName ?? source.Name);
			dest.SyncFrom(source);
		}

		protected virtual void SyncCopy(Record source)
		{
			if (source.IsFile)
			{
				Write(source.Read());
				return;
			}

			foreach (var record in source.Records)
			{
				Record dest;
				if (record.IsFile)
					dest = CreateFile(record.Name);
				else
					dest = CreateDirectory(record.Name);
				dest.SyncCopy(record);
			}
		}

		string GetKey(Record record)
		{
			return String.Format("{0}-{1}-{2}-{3}", record.IsFile, record.Name, record[RecordProperty.PropertyName.Size], record[RecordProperty.PropertyName.WriteTime]);
		}

		public void SyncFrom(Record source)
		{
			if ((IsFile) && (source.IsFile))
				return;

			var sourceRecords = new Dictionary<string, Record>();
			foreach (var record in source.Records)
				sourceRecords[GetKey(record)] = record;

			var destRecords = new Dictionary<string, Record>();
			foreach (var record in Records)
				destRecords[GetKey(record)] = record;

			var dups = sourceRecords.Keys.Where(key => destRecords.Keys.Contains(key)).ToList();
			foreach (var dup in dups)
			{
				destRecords[dup].SyncFrom(sourceRecords[dup]);
				sourceRecords.Remove(dup);
				destRecords.Remove(dup);
			}

			foreach (var dest in destRecords)
				dest.Value.Delete();

			foreach (var record in sourceRecords.Values)
			{
				Record dest;
				if (record.IsFile)
					dest = CreateFile(record.Name);
				else
					dest = CreateDirectory(record.Name);
				dest.SyncCopy(record);
			}
		}

		public virtual BinaryData Read() { throw new NotImplementedException(); }
		public virtual void Write(BinaryData data) { throw new NotImplementedException(); }

		public virtual Type GetRootType() { return typeof(Record); }

		public virtual Record GetRecord(string uri)
		{
			string findUri = "", remaining = uri;
			var record = this as Record;
			while (record != null)
			{
				if (uri.Equals(record.FullName, StringComparison.OrdinalIgnoreCase))
					return record;

				var match = Regex.Match(remaining, @"^(\\*[^\\]+)(.*)$");
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
