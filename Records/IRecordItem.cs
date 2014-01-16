using System;

namespace NeoEdit.Records
{
	public interface IRecordItem : IRecord
	{
		Int64 Size { get; }
		byte[] Read(Int64 position, int bytes);
	}
}
