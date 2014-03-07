using System.Collections.Generic;
using NeoEdit.Win32Interop;

namespace NeoEdit.GUI.Records.Handles
{
	public class HandleRoot : HandleRecord
	{
		public HandleRoot() : base("Handles") { }

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var type in Interop.GetHandleTypes())
					yield return new HandleType(type);
			}
		}
	}
}
