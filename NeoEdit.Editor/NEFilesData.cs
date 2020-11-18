using NeoEdit.Common;

namespace NeoEdit.Editor
{
	class NEFilesData
	{
		public IReadOnlyOrderedHashSet<NEFile> allFiles;
		public IReadOnlyOrderedHashSet<NEFile> activeFiles;
		public NEFile focused;
		public WindowLayout windowLayout;
		public bool activeOnly;
		public bool macroVisualize = true;

		public NEFilesData Clone() => new NEFilesData
		{
			allFiles = allFiles,
			activeFiles = activeFiles,
			focused = focused,
			windowLayout = windowLayout,
			activeOnly = activeOnly,
			macroVisualize = macroVisualize,
		};
	}
}
