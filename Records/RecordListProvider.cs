using System;
using System.Linq;

namespace NeoEdit.Records
{
	public static class RecordListProvider
	{
		public static IRecord GetRecord(string uri, IRecordRoot root)
		{
			var parts = uri.Split('\\').ToList();
			for (var ctr1 = parts.Count; ctr1 >= 0; --ctr1)
				for (var ctr2 = ctr1 + 1; ctr2 < parts.Count; ++ctr2)
					parts[ctr2] = parts[ctr1] + @"\" + parts[ctr2];

			IRecord record = root;
			while (record != null)
			{
				if (uri.Equals(record.FullName, StringComparison.OrdinalIgnoreCase))
					return record;

				if ((parts.Count == 0) || (!(record is IRecordList)))
					break;
				var part = parts[0];
				parts.RemoveAt(0);

				record = (record as IRecordList).Records.SingleOrDefault(a => a.FullName.Equals(part, StringComparison.OrdinalIgnoreCase));
			}

			throw new Exception(String.Format("Invalid input: {0}", uri));
		}

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

		public static IRecordList GetRecordList(string uri, IRecordList defaultList = null)
		{
			uri = SimplifyURI(uri);

			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(a => (!a.IsInterface) && (typeof(IRecordRoot).IsAssignableFrom(a))).ToList();
			foreach (var type in types)
			{
				var root = Activator.CreateInstance(type) as IRecordRoot;
				try
				{
					var record = root.GetRecord(uri);
					if (record == null)
						continue;
					if (record is IRecordItem)
						record = record.Parent;
					return record as IRecordList;
				}
				catch { }
			}

			if (defaultList != null)
				return defaultList;

			return new RootRecordList();
		}
	}
}
