﻿using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class Root : Record
	{
		public Root() : base("Root") { }

		public override Record GetRecord(string uri)
		{
			foreach (var record in Records)
			{
				try
				{
					var tempRecord = record.GetRecord(uri);
					if (tempRecord != null)
						return tempRecord;
				}
				catch { }
			}
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				yield return new Disks.DiskRoot();
				yield return new Lists.ListRoot();
				yield return new Registries.RegistryRoot();
				yield return new Handles.HandleRoot();
			}
		}
	}
}
