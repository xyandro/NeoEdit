using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Win32;

namespace NeoEdit.Records.Handles
{
	public class HandleType : HandleRecord
	{
		public HandleType(string uri) : base(String.Format(@"Handles\{0}", uri)) { }

		public override Record Parent { get { return new HandleRoot(); } }

		public override IEnumerable<Record> Records
		{
			get
			{
				var handles = Interop.GetAllHandles();
				Interop.GetTypeHandles(handles, Name);
				return Interop.GetHandleInfo(handles).Select(handle => new HandleItem(handle));
			}
		}
	}
}
