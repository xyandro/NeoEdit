using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.Records
{
	public abstract class RecordList : Record
	{
		protected RecordList(string uri, RecordList parent) : base(uri, parent) { records = new ObservableCollection<Record>(); }
		protected virtual IEnumerable<Record> InternalRecords { get { return new List<Record>(); } }
		readonly ObservableCollection<Record> records;
		public ObservableCollection<Record> Records { get { Refresh(); return records; } }

		public void Refresh()
		{
			var existingList = records.ToDictionary(a => a.FullName, a => a);
			var newList = InternalRecords.ToDictionary(a => a.FullName, a => a);

			var toAdd = newList.Where(a => !existingList.Keys.Contains(a.Key));
			foreach (var add in toAdd)
				records.Add(add.Value);

			var toRemove = existingList.Where(a => !newList.Keys.Contains(a.Key));
			foreach (var remove in toRemove)
				records.Remove(remove.Value);
		}
	}
}
