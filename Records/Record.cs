using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		public delegate void PropertyChangedDelegate(RecordProperty.PropertyName property, object value);
		public event PropertyChangedDelegate PropertyChanged = delegate { };

		Dictionary<RecordProperty.PropertyName, object> properties = new Dictionary<RecordProperty.PropertyName, object>();

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

		public IEnumerable<RecordProperty.PropertyName> Properties { get { return properties.Keys; } }
		public IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				var actions = new List<RecordAction.ActionName>();
				actions.Add(RecordAction.ActionName.Sync);
				if (CanCopy()) actions.Add(RecordAction.ActionName.Copy);
				if (CanCut()) actions.Add(RecordAction.ActionName.Cut);
				if (CanPaste()) actions.Add(RecordAction.ActionName.Paste);
				if (CanMD5()) actions.Add(RecordAction.ActionName.MD5);
				if (CanIdentify()) actions.Add(RecordAction.ActionName.Identify);
				if (CanSyncSource()) actions.Add(RecordAction.ActionName.SyncSource);
				if (CanSyncTarget()) actions.Add(RecordAction.ActionName.SyncTarget);
				if (CanOpen()) actions.Add(RecordAction.ActionName.Open);
				if (CanView()) actions.Add(RecordAction.ActionName.View);
				return actions;
			}
		}
		protected T GetProperty<T>(RecordProperty.PropertyName property) { return properties.ContainsKey(property) ? (T)properties[property] : default(T); }

		protected virtual void SetProperty<T>(RecordProperty.PropertyName property, T value)
		{
			if (value == null)
				properties.Remove(property);
			else
				properties[property] = value;
			PropertyChanged(property, value);

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

		public virtual bool CanRename() { return false; }
		public virtual bool CanDelete() { return false; }
		public virtual bool CanCopy() { return false; }
		public virtual bool CanCut() { return false; }
		public virtual bool CanPaste() { return false; }
		public virtual bool CanMD5() { return false; }
		public virtual bool CanIdentify() { return false; }
		public virtual bool CanSyncSource() { return false; }
		public virtual bool CanSyncTarget() { return false; }
		public virtual bool CanOpen() { return false; }
		public virtual bool CanView() { return false; }

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

		protected virtual void CopyFrom(Record source)
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
				dest.CopyFrom(record);
			}
		}

		string GetKey(Record record, SyncParams syncParams)
		{
			switch (syncParams.Type)
			{
				case SyncParams.SyncType.NameSizeLength:
					long ticks = 0;
					var time = record[RecordProperty.PropertyName.WriteTime];
					if (time is DateTime)
						ticks = ((DateTime)time).Ticks;
					return String.Format("{0}-{1}-{2}-{3}", record.IsFile, record.Name, record[RecordProperty.PropertyName.Size], ticks);
				default: throw new NotImplementedException();
			}
		}

		bool DoSyncFrom(Record source, SyncParams syncParams, Action<string> logger)
		{
			var hasErrors = false;
			if ((IsFile) && (source.IsFile))
				return hasErrors;

			var sourceRecords = new Dictionary<string, Record>();
			foreach (var record in source.Records)
				sourceRecords[GetKey(record, syncParams)] = record;

			var destRecords = new Dictionary<string, Record>();
			foreach (var record in Records)
				destRecords[GetKey(record, syncParams)] = record;

			var dups = sourceRecords.Keys.Where(key => destRecords.Keys.Contains(key)).ToList();
			foreach (var dup in dups)
			{
				if (destRecords[dup].DoSyncFrom(sourceRecords[dup], syncParams, logger))
					hasErrors = true;
				sourceRecords.Remove(dup);
				destRecords.Remove(dup);
			}

			foreach (var dest in destRecords)
				try
				{
					logger(String.Format("Deleting {0}", dest.Value.FullName));
					hasErrors = true;
					if (!syncParams.LogOnly)
						dest.Value.Delete();
				}
				catch (Exception ex)
				{
					logger(ex.Message);
					hasErrors = true;
					if (syncParams.StopOnError)
						throw;
				}

			foreach (var record in sourceRecords.Values)
			{
				Record dest;
				if (record.IsFile)
					dest = CreateFile(record.Name);
				else
					dest = CreateDirectory(record.Name);

				try
				{
					logger(String.Format("Copying {0} to {1}", record.FullName, dest.FullName));
					if (!syncParams.LogOnly)
						dest.CopyFrom(record);
				}
				catch (Exception ex)
				{
					logger(ex.Message);
					hasErrors = true;
					if (syncParams.StopOnError)
						throw;
				}
			}

			return hasErrors;
		}

		public void SyncFrom(Record source, SyncParams syncParams = null, Action<string> logger = null)
		{
			if (syncParams == null)
				syncParams = new SyncParams();
			if (logger == null)
				logger = str => { };

			logger(String.Format("Beginning sync from {0} to {1}", source.FullName, FullName));
			try
			{
				bool hasErrors = false;
				if ((source.IsFile) && (IsFile))
					CopyFrom(source);
				else
					if (DoSyncFrom(source, syncParams, logger))
						hasErrors = true;
				if (hasErrors)
					logger("Finished with errors.");
				else
					logger("Finished.");
			}
			catch
			{
				logger("Error.");
			}
		}

		public virtual byte[] Read() { throw new NotImplementedException(); }
		public virtual void Write(byte[] data) { throw new NotImplementedException(); }

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
