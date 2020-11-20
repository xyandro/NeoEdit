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

		public NEWindowData Clone() => new NEWindowData(neWindow)
		{
			neFileDatas = neFileDatas,
			activeFiles = activeFiles,
			focused = focused,
			windowLayout = windowLayout,
			activeOnly = activeOnly,
			macroVisualize = macroVisualize,
		};

		public override string ToString() => NESerial.ToString();
	}
}
