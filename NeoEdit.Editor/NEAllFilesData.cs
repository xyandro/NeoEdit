using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEAllFilesData
	{
		public readonly int NESerial = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<NEFilesData> allNEFilesData;

		public NEAllFilesData Clone() => new NEAllFilesData { allNEFilesData = allNEFilesData };
	}
}
