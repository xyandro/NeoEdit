using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData
	{
		public readonly int NESerial = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<NEWindowData> neWindowDatas;

		public NEGlobalData Clone() => new NEGlobalData { neWindowDatas = neWindowDatas };
	}
}
