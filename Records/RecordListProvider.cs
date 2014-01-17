using System;
using System.Linq;

namespace NeoEdit.Records
{
	public static class RecordListProvider
	{
		static string SimplifyURI(string name)
		{
			name = name.Replace("/", "\\");
			while (true)
			{
				var oldName = name;
				name = name.Replace("\\\\", "\\");
				name = name.Trim().TrimEnd('\\');
				if ((name.StartsWith("\"")) && (name.EndsWith("\"")))
					name = name.Substring(1, name.Length - 2);
				if (oldName == name)
					break;
			}
			return name;
		}

		public static RecordList GetRecordList(string uri, RecordList defaultList = null)
		{
			uri = SimplifyURI(uri);

			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(a => (!a.IsAbstract) && (typeof(RecordRoot).IsAssignableFrom(a))).ToList();
			foreach (var type in types)
			{
				var root = Activator.CreateInstance(type) as RecordRoot;
				try
				{
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
