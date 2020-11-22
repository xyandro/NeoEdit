using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData : INEGlobalData
	{
		public int NESerial { get; } = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<INEWindowData> NEWindowDatas { get; set; }
		public IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; set; }

		public NEGlobalData()
		{
			NEWindowDatas = new OrderedHashSet<INEWindowData>();
			NEWindows = new OrderedHashSet<NEWindow>();
		}

		public NEGlobalData(INEGlobalData neGlobalData)
		{
			NEWindowDatas = neGlobalData.NEWindowDatas;
			NEWindows = neGlobalData.NEWindows;
		}

		public override string ToString() => NESerial.ToString();
	}
}
