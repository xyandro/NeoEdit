using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Win32Interop;

namespace NeoEdit.GUI.Records.Handles
{
	public class HandleType : HandleRecord
	{
		public HandleType(string uri) : base(String.Format(@"Handles\{0}", uri)) { }

		public override Record Parent { get { return new HandleRoot(); } }

		public override IEnumerable<Record> Records { get { return Interop.GetTypeHandles(Name).Select(handle => new HandleItem(handle)); } }
	}
}
