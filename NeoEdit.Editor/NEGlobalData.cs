using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalData : INEGlobalData
	{
		public long NESerial { get; } = NESerialTracker.NESerial;

		public IReadOnlyOrderedHashSet<INEWindowData> NEWindowDatas { get; set; }
		public IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; set; }

		public NEGlobalData()
		{
			NEWindowDatas = new OrderedHashSet<INEWindowData>();
			NEWindows = new OrderedHashSet<NEWindow>();
		}

		public INEGlobalData Next()
		{
			return new NEGlobalData
			{
				NEWindowDatas = NEWindowDatas,
				NEWindows = NEWindows,
			};
		}

		public override string ToString() => NESerial.ToString();
	}
}
