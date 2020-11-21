using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData
	{
		public readonly int NESerial = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<NEWindowData> neWindowDatas;

		public NEGlobalData() { }

		public NEGlobalData(NEGlobalData neGlobalData) => neWindowDatas = neGlobalData.neWindowDatas;

		public override string ToString() => NESerial.ToString();
	}
}
