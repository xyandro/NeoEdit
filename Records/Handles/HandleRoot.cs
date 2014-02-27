using System.Collections.Generic;
using System.Linq;
using NeoEdit.Interop;

namespace NeoEdit.Records.Handles
{
	public class HandleRoot : HandleRecord
	{
		public HandleRoot() : base("Handles") { }

		public override IEnumerable<Record> Records { get { return NEInterop.GetHandles().Select(handle => new HandleItem(handle)); } }
	}
}
