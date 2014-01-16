using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeoEdit.Records
{
	public static class RecordListProvider
	{
		static List<Func<string, IRecordList>> providers = new List<Func<string, IRecordList>>();
		static RecordListProvider()
		{
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(a => (!a.IsInterface) && (typeof(IRecordList).IsAssignableFrom(a))).ToList();
			foreach (var type in types)
			{
				var method = type.GetMethod("get_Provider", BindingFlags.Static | BindingFlags.NonPublic);
				if (method != null)
				{
					var provider = method.Invoke(null, new object[0]) as Func<string, IRecordList>;
					if (provider != null)
						providers.Add(provider);
				}
			}
		}

		public static IRecordList GetRecordList(string recordUri)
		{
			foreach (var provider in providers)
			{
				var recordList = provider(recordUri);
				if (recordList != null)
					return recordList;
			}
			return new RootRecordList();
		}
	}
}
