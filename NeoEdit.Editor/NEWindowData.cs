using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEWindow neWindow;

		public IReadOnlyOrderedHashSet<NEFileData> allFileDatas;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;
		public WindowLayout windowLayout;
		public bool activeOnly;
		public bool macroVisualize = true;

		public NEWindowData(NEWindow neWindow) => this.neWindow = neWindow;

		public NEWindowData Clone() => new NEWindowData(neWindow)
		{
			allFileDatas = allFileDatas,
			activeFiles = activeFiles,
			focused = focused,
			windowLayout = windowLayout,
			activeOnly = activeOnly,
			macroVisualize = macroVisualize,
		};
	}
}
