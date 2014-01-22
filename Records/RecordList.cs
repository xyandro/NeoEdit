using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NeoEdit.Records
{
	public abstract class RecordList : Record
	{
		protected RecordList(string uri, RecordList parent) : base(uri, parent) { records = new ObservableCollection<Record>(); }
		protected virtual IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords { get { return new List<Tuple<string, Func<string, Record>>>(); } }
		readonly ObservableCollection<Record> records;
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
