using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData
	{
		public readonly int NESerial = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<NEFilesData> allNEFilesData;

		public NEGlobalData Clone() => new NEGlobalData { allNEFilesData = allNEFilesData };
	}
}
