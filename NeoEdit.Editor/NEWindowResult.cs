using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowResult
	{
		public List<NEWindow> NEWindows { get; private set; }
		public List<NEFile> NewNEFiles { get; private set; }
		public NEClipboard Clipboard { get; private set; }
		public IReadOnlyList<KeysAndValues>[] KeysAndValues { get; private set; }
		public IReadOnlyList<string> DragFiles { get; private set; }

		public void ClearNEWindows()
		{
			if (NEWindows == null)
				NEWindows = new List<NEWindow>();
			NEWindows.Clear();
		}

		public void AddNewNEFile(NEFile neFile)
		{
			if (NewNEFiles == null)
				NewNEFiles = new List<NEFile>();
			NewNEFiles.Add(neFile);
		}

		public void SetClipboard(NEClipboard clipboard) => Clipboard = clipboard;

		public void SetKeysAndValues(IReadOnlyList<KeysAndValues>[] keysAndValues) => KeysAndValues = keysAndValues;

		public void SetDragFiles(IReadOnlyList<string> dragFiles) => DragFiles = dragFiles;
	}
}
