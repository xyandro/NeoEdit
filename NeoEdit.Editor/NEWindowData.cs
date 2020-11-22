using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEWindow neWindow;

		public IReadOnlyOrderedHashSet<NEFileData> neFileDatas;
		public IReadOnlyOrderedHashSet<NEFile> neFiles;
		public IReadOnlyOrderedHashSet<NEFile> orderedNEFiles;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;

		public NEWindowData(NEWindow neWindow)
		{
			this.neWindow = neWindow;
			neFileDatas = new OrderedHashSet<NEFileData>();
			neFiles = orderedNEFiles = activeFiles = new OrderedHashSet<NEFile>();
		}

		public NEWindowData(NEWindowData neWindowData)
		{
			neWindow = neWindowData.neWindow;

			neFileDatas = neWindowData.neFileDatas;
			neFiles = neWindowData.neFiles;
			orderedNEFiles = neWindowData.orderedNEFiles;
			activeFiles = neWindowData.activeFiles;
			focused = neWindowData.focused;
		}

		public override string ToString() => NESerial.ToString();
	}
}
