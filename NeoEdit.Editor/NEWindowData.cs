using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEWindow neWindow;

		public IReadOnlyOrderedHashSet<NEFileData> neFileDatas;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;
		public WindowLayout windowLayout;
		public bool activeOnly;
		public bool macroVisualize = true;

		public NEWindowData(NEWindow neWindow) => this.neWindow = neWindow;

		public NEWindowData(NEWindowData neWindowData)
		{
			neFileDatas = neWindowData.neFileDatas;
			activeFiles = neWindowData.activeFiles;
			focused = neWindowData.focused;
			windowLayout = neWindowData.windowLayout;
			activeOnly = neWindowData.activeOnly;
			macroVisualize = neWindowData.macroVisualize;
		}

		public override string ToString() => NESerial.ToString();
	}
}
