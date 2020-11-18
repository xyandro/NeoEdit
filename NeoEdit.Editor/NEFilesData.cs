using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFilesData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEFiles neFiles;

		public IReadOnlyOrderedHashSet<NEFileData> allFileDatas;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;
		public WindowLayout windowLayout;
		public bool activeOnly;
		public bool macroVisualize = true;

		public NEFilesData(NEFiles neFiles) => this.neFiles = neFiles;

		public NEFilesData Clone() => new NEFilesData(neFiles)
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
