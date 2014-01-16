using System.Collections.Generic;

namespace NeoEdit.Records
{
	public interface IRecordList : IRecord
	{
		IEnumerable<IRecord> Records { get; }
	}
}
