using System;
using System.Linq;

namespace NeoEdit.Records
{
	public static class RecordListProvider
	{
		public static RecordList GetRecordList(string uri, RecordList defaultList = null)
		{
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(a => (!a.IsAbstract) && (typeof(RecordRoot).IsAssignableFrom(a))).ToList();
			foreach (var type in types)
			{
				try
				{
					var root = Activator.CreateInstance(type) as RecordRoot;
					if (root.FullName.Equals(uri, StringComparison.OrdinalIgnoreCase))
						return root;
					var record = root.GetRecord(uri);
					if (record == null)
						continue;
					if (record is RecordItem)
						record = record.Parent;
					return record as RecordList;
				}
				catch { }
			}

			if (defaultList != null)
				return defaultList;

			return new RootRecordList();
		}
	}
}
