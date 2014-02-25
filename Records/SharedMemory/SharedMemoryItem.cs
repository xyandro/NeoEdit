using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Records.SharedMemory
{
	public class SharedMemoryItem : SharedMemoryRecord
	{
		public SharedMemoryItem(string uri) : base(uri) { }

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName>
				{
					RecordAction.ActionName.Open,
				}
				.Concat(base.Actions);
			}
		}

		public override BinaryData Read()
		{
			return new SharedMemoryBinaryData(FullName);
		}
	}
}
