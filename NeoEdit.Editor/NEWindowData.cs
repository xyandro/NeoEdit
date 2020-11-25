using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData : INEWindowData
	{
		public int NESerial { get; } = NESerialTracker.NESerial;
		public NEWindow NEWindow { get; }

		public IReadOnlyOrderedHashSet<INEFileData> NEFileDatas { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> NEFiles { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> OrderedNEFiles { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles { get; set; }
		public NEFile Focused { get; set; }

		public NEWindowData(NEWindow neWindow)
		{
			NESerial = int.MinValue;
			NEWindow = neWindow;
			NEFileDatas = new OrderedHashSet<INEFileData>();
			NEFiles = OrderedNEFiles = ActiveFiles = new OrderedHashSet<NEFile>();
		}

		public NEWindowData(INEWindowData neWindowData)
		{
			NEWindow = neWindowData.NEWindow;

			NEFileDatas = neWindowData.NEFileDatas;
			NEFiles = neWindowData.NEFiles;
			OrderedNEFiles = neWindowData.OrderedNEFiles;
			ActiveFiles = neWindowData.ActiveFiles;
			Focused = neWindowData.Focused;
		}

		public override string ToString() => NESerial.ToString();
	}
}
