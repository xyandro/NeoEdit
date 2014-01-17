﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : RecordList
	{
		public RegistryDir(string uri, RecordList parent) : base(uri, parent) { }

		public override IEnumerable<Record> Records
		{
			get
			{
				using (var subKey = RegistryHelpers.GetKey(FullName))
				{
					foreach (var name in subKey.GetSubKeyNames())
						yield return new RegistryDir(FullName + @"\" + name, this);
					foreach (var name in subKey.GetValueNames())
						yield return new RegistryFile(FullName + @"\" + name, this);
				}
			}
		}
	}
}
