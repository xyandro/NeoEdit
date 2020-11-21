using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData
	{
		public readonly int NESerial = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<NEWindowData> neWindowDatas;
		public IReadOnlyOrderedHashSet<NEWindow> neWindows;

		public NEGlobalData()
		{
			neWindowDatas = new OrderedHashSet<NEWindowData>();
			neWindows = new OrderedHashSet<NEWindow>();
		}

		public NEGlobalData(NEGlobalData neGlobalData)
		{
			neWindowDatas = neGlobalData.neWindowDatas;
			neWindows = neGlobalData.neWindows;
		}

		public override string ToString() => NESerial.ToString();
	}
}
