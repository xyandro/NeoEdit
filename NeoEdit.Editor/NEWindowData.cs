using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData : INEWindowData
	{
		public int NESerial { get; } = NESerialTracker.NESerial;
		public NEWindow NEWindow { get; }

		public IReadOnlyOrderedHashSet<INEFileData> NEFileDatas { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> NEFiles { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles { get; set; }
		public NEFile Focused { get; set; }

		public NEWindowData(NEWindow neWindow)
		{
			NEWindow = neWindow;
			NEFileDatas = new OrderedHashSet<INEFileData>();
			NEFiles = ActiveFiles = new OrderedHashSet<NEFile>();
		}

		public NEWindowData(INEWindowData neWindowData)
		{
			NEWindow = neWindowData.NEWindow;

			NEFileDatas = neWindowData.NEFileDatas;
			NEFiles = neWindowData.NEFiles;
			ActiveFiles = neWindowData.ActiveFiles;
			Focused = neWindowData.Focused;
		}

		public override string ToString() => NESerial.ToString();
	}
}
