using System.Collections.Generic;
using System.Linq;
using NeoEdit.Interop;

namespace NeoEdit.Records.SharedMemory
{
	public class SharedMemoryRoot : SharedMemoryRecord
	{
		public SharedMemoryRoot() : base("Shared Memory") { }

		public override IEnumerable<Record> Records { get { return NEInterop.GetSharedMemoryNames().Select(name => new SharedMemoryItem(name)); } }
	}
}
