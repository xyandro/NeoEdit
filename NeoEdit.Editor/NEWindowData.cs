using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEWindow neWindow;

		public IReadOnlyOrderedHashSet<NEFileData> neFileDatas;
		public IReadOnlyOrderedHashSet<NEFile> neFiles;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;
		public WindowLayout windowLayout;
		public bool activeOnly;
		public bool macroVisualize = true;

		public NEWindowData(NEWindow neWindow)
		{
			this.neWindow = neWindow;
			neFileDatas = new OrderedHashSet<NEFileData>();
			neFiles = activeFiles = new OrderedHashSet<NEFile>();
			windowLayout = new WindowLayout(1, 1);
		}

		public NEWindowData(NEWindowData neWindowData)
		{
			neWindow = neWindowData.neWindow;

			neFileDatas = neWindowData.neFileDatas;
			neFiles = neWindowData.neFiles;
			activeFiles = neWindowData.activeFiles;
			focused = neWindowData.focused;
			windowLayout = neWindowData.windowLayout;
			activeOnly = neWindowData.activeOnly;
			macroVisualize = neWindowData.macroVisualize;
		}

		public override string ToString() => NESerial.ToString();
	}
}
